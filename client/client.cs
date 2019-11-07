using System;
using System.IO;
using Grpc.Net.Client;
using System.Numerics;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;
using Google.Protobuf;

namespace XChainSDK
{
    partial class XChainClient
    {
        private const string SwitchName = "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport";

        // internal members
        private XCAccount account;
        private Pb.Xchain.XchainClient client;

        // public properties
        // user account info
        public XCAccount Account
        {
            get
            {
                return account;
            }
            set
            {
                account = value;
            }
        }

        public bool Init(string keypath, string targetHost)
        {
            try
            {
                AppContext.SetSwitch(SwitchName, true);
                var channel = GrpcChannel.ForAddress("http://" + targetHost);
                this.client = new Pb.Xchain.XchainClient(channel);
                this.account = new XCAccount();
                return SetAccountByPath(keypath);
            }
            catch (Exception e)
            {
                Console.WriteLine("Init failed, err=" + e.ToString());
                return false;
            }
        }

        public bool SetAccountByPath(string path)
        {
            try
            {
                using (StreamReader sr = new StreamReader(path + "/address"))
                {
                    var addr = sr.ReadToEnd();
                    this.account.Address = addr;
                }
                using (StreamReader sr = new StreamReader(path + "/private.key"))
                {
                    var privkeyStr = sr.ReadToEnd();
                    var privkey = new AccountPrivateKey();
                    privkey.ParseJSON(privkeyStr);
                    this.account.PrivateKey = privkey;
                }
                using (StreamReader sr = new StreamReader(path + "/public.key"))
                {
                    var pubkeyStr = sr.ReadToEnd();
                    var pubkey = new AccountPublicKey();
                    pubkey.ParseJSON(pubkeyStr);
                    this.account.PublicKey = pubkey;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in SetAccountByPath, err=" + e);
                return false;
            }

            return true;
        }

        public BigInteger GetBalance(string bcname, string address)
        {
            if (client == null)
            {
                throw new NullReferenceException();
            }
            var reqData = new Pb.AddressStatus
            {
                Address = address,
            };
            var tokenDetail = new Pb.TokenDetail
            {
                Bcname = bcname,
            };
            reqData.Bcs.Add(tokenDetail);
            var res = client.GetBalance(reqData);
            if (res.Header.Error != Pb.XChainErrorEnum.Success)
            {
                throw new Exception(res.Header.Error.ToString())
                {
                    HResult = (int)res.Header.Error,
                };
            }
            var balanceStr = res.Bcs[0].Balance;
            var balance = BigInteger.Parse(balanceStr);
            return balance;
        }

        public BigInteger GetBalance(string bcname)
        {
            return GetBalance(bcname, Account.Address);
        }

        public Response Transfer(string bcname, string to, BigInteger amount, string desc)
        {
            var response = new Response()
            {
                Error = new XChainError()
                {
                    ErrorCode = Errors.Success,
                    ErrorMessage = "Success",
                }
            };
            var utxo = SelectUTXO(bcname, this.Account.Address, this.Account.PublicKey, amount);
            if (utxo == null)
            {
                Console.WriteLine("Select utxo failed");
                return null;
            }
            var tx = AssembleTx(utxo, this.Account, null, to, amount, null, desc);
            if (tx == null)
            {
                Console.WriteLine("AssembleTx failed");
                return null;
            }
            // post transaction
            var postRes = PostTx(bcname, tx);
            if (postRes == null || postRes.Header.Error != Pb.XChainErrorEnum.Success)
            {
                Console.WriteLine("InvokeContract: PostTx failed");
                return null;
            }
            response.Txid = XConvert.ByteArrayToHexString(tx.Txid).ToLower();
            return response;
        }

        // Query a transaction with transaction id
        /// <param name="txid">transaction id</param> 
        /// <return>the request response, Data is a Transaction type</return>
        public Response QueryTx(string bcname, string txid)
        {
            var response = new Response()
            {
                Error = new XChainError()
                {
                    ErrorCode = Errors.Success,
                    ErrorMessage = "Success",
                }
            };
            var req = new Pb.TxStatus
            {
                Header = GetDefaultHeader(),
                Bcname = bcname,
                Txid = ByteString.CopyFrom(XConvert.HexStringToByteArray(txid)),
            };
            var tx = client.QueryTx(req);
            if (tx == null || tx.Header.Error != Pb.XChainErrorEnum.Success)
            {
                Console.WriteLine("query tx failed. errcode=" + (int)tx.Header.Error + ", logid=" + req.Header.Logid);
                return null;
            }
            response.Data = XConvert.PbTxToLocalTx(tx.Tx);
            return response;
        }

        // Create contract account using given accountName
        // Note that `accountName` is a 16-digits string, like "0000000000000001".
        // Customized ACL is not supproted currently.
        public Response NewContractAccount(string bcname, string accountName)
        {
            var args = new SortedDictionary<string, byte[]>();
            args["account_name"] = Encoding.ASCII.GetBytes(accountName);
            args["acl"] = Encoding.ASCII.GetBytes("{\"pm\":{\"rule\":1,\"acceptValue\":1.0},\"aksWeight\":{\"" +
                this.Account.Address + "\":1.0}}");
            var invokeRes = InvokeContract(bcname, null, "NewAccount", args, null, "", ContactVMType.Type.XKernel);
            if (invokeRes == null || invokeRes.Error.ErrorCode != Errors.Success)
            {
                Console.WriteLine("NewContractAccount failed");
                return null;
            }
            return new Response
            {
                Error = new XChainError
                {
                    ErrorCode = Errors.Success,
                },
                Data = "XC" + accountName + "@" + bcname,
            };
        }

        // DeployWASMContract deploy a WASM contract.
        /// <param name="contractName">the name of contract to deploy, unique on blockchain</param> 
        /// <param name="path">the path of built WASM binary</param> 
        /// <param name="accountName">deploy contract using which contract account</param> 
        /// <param name="initArgs">initializing arguments of contract</param> 
        /// <param name="runtime">runtime of the contract, "c" for C/C++, "go" for Golang</param> 
        public Response DeployWASMContract(string bcname, string contractName, string path, string accountName,
             Dictionary<string, byte[]> initArgs, string runtime, string desc = "")
        {
            var args = new SortedDictionary<string, byte[]>();
            using (StreamReader sr = new StreamReader(path))
            {
                args["account_name"] = Encoding.ASCII.GetBytes(accountName);
                args["contract_name"] = Encoding.ASCII.GetBytes(contractName);
                using (var ms = new MemoryStream())
                {
                    sr.BaseStream.CopyTo(ms);
                    args["contract_code"] = ms.ToArray();
                }
                using (var ms = new MemoryStream())
                {
                    var contractDesc = new Pb.WasmCodeDesc
                    {
                        Runtime = runtime,
                    };
                    contractDesc.WriteTo(ms);
                    args["contract_desc"] = ms.ToArray();
                }
                args["init_args"] = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(initArgs));
                var authRequire = new List<string>();
                authRequire.Add(accountName + "/" + this.Account.Address);
                var invokeRes = InvokeContract(bcname, contractName, "Deploy", args, authRequire, desc, ContactVMType.Type.XKernel);
                if (invokeRes == null || invokeRes.Error.ErrorCode != Errors.Success)
                {
                    Console.WriteLine("DeployContract failed");
                    return null;
                }
                return new Response
                {
                    Error = new XChainError
                    {
                        ErrorCode = Errors.Success,
                    },
                    Data = invokeRes,
                };
            }
        }

        // Invokes contract method with given args
        // TODO: multisig is not supported
        /// <param name="contractName">the name of contract to invoke</param> 
        /// <param name="method">the method name of contract to invoke</param> 
        /// <param name="args">the arguments of contract</param> 
        /// <param name="authRequire">add more address if multisig needed, otherwise keep null</param> 
        public Response InvokeContract(string bcname, string contractName, string method,
            SortedDictionary<string, byte[]> args, List<string> authRequire = null, string desc = "",
            ContactVMType.Type contractType = ContactVMType.Type.WASM)
        {
            // pre-execute contract
            var execRes = PreExecWithSelectUTXO(bcname, this.Account.Address,
                this.Account.PrivateKey, 0, contractName, method, args, contractType,
                this.Account.Address, authRequire);
            if (execRes == null)
            {
                Console.WriteLine("InvokeContract: PreExecWithSelectUTXO failed");
                return null;
            }
            // check contract response
            var contractResult = new Dictionary<string, string>();
            for (var i = 0; i < execRes.Response.Responses.Count; i++)
            {
                if (execRes.Response.Responses[i].Status >= 400)
                {
                    Console.WriteLine("Contract execute failed. res=" +
                        JsonConvert.SerializeObject(execRes.Response.Responses[i]));
                    return new Response
                    {
                        Error = new XChainError
                        {
                            ErrorCode = Errors.Success,
                        },
                    };
                }
                contractResult.Add(execRes.Response.Requests[i].ContractName + ":" + execRes.Response.Requests[i].MethodName,
                    Encoding.ASCII.GetString(execRes.Response.Responses[i].Body.ToByteArray()));
            }

            // assemble transaction
            var tx = AssembleTx(execRes.UtxoOutput, this.Account, authRequire, "", 0, execRes.Response, desc);
            if (tx == null)
            {
                Console.WriteLine("InvokeContract: AssembleTx failed");
                return null;
            }

            // post transaction
            var postRes = PostTx(bcname, tx);
            if (postRes == null || postRes.Header.Error != Pb.XChainErrorEnum.Success)
            {
                Console.WriteLine("InvokeContract: PostTx failed");
                return null;
            }
            var res = new Response
            {
                Error = new XChainError
                {
                    ErrorCode = Errors.Success,
                },
                Txid = XConvert.ByteArrayToHexString(tx.Txid),
                Data = contractResult,
            };
            return res;
        }
    }
}