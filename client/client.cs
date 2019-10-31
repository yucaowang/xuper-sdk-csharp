using System;
using System.IO;
using Pb;
using Grpc.Net.Client;
using System.Numerics;
using System.Text;

namespace XChainSDK
{
    class XChainClient
    {
        private const string SwitchName = "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport";

        // internal members
        private XCAccount account;
        private Xchain.XchainClient client;

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
                this.client = new Xchain.XchainClient(channel);
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
            var reqData = new Pb.UtxoInput()
            {
                Bcname = bcname,
                Address = this.Account.Address,
                Publickey = this.Account.PublicKey.RawKey,
                TotalNeed = amount.ToString(),
                NeedLock = false,
            };
            var res = client.SelectUTXO(reqData);
            if (res.Header.Error != Pb.XChainErrorEnum.Success)
            {
                Console.WriteLine("select utxo failed, err=", res.Header.Error);
                response.Error.ErrorCode = Errors.SelectUTXOError;
                response.Error.ErrorMessage = "Select UTXO error";
                return response;
            }
            var tx = new Transaction();
            tx.TxInputs = new TxInput[res.UtxoList.Count];
            for (var i = 0; i < res.UtxoList.Count; i++)
            {
                var utxo = res.UtxoList[i];
                var input = new TxInput
                {
                    FromAddr = utxo.ToAddr.ToByteArray(),
                    Amount = utxo.Amount.ToByteArray(),
                    RefTxid = utxo.RefTxid.ToByteArray(),
                    RefOffset = utxo.RefOffset,
                };
                tx.TxInputs[i] = input;
            }

            var outputLen = 1;
            var total = BigInteger.Parse(res.TotalSelected);
            if (total > amount)
            {
                outputLen++;
            }
            tx.TxOutputs = new TxOutput[outputLen];
            var output = new TxOutput()
            {
                ToAddr = Encoding.ASCII.GetBytes(to),
                Amount = amount.ToByteArray(),
            };
            Array.Reverse(output.Amount, 0, output.Amount.Length);
            tx.TxOutputs[0] = output;
            if (total > amount)
            {
                var charge = total - amount;
                var chargeOutput = new TxOutput()
                {
                    ToAddr = Encoding.ASCII.GetBytes(this.Account.Address),
                    Amount = charge.ToByteArray(),
                };
                Array.Reverse(chargeOutput.Amount, 0, chargeOutput.Amount.Length);
                tx.TxOutputs[1] = chargeOutput;
            }
            tx.Desc = Encoding.ASCII.GetBytes(desc);
            tx.Version = 1;
            tx.Coinbase = false;
            tx.Autogen = false;
            tx.Initiator = this.Account.Address;
            var digestHash = XDigest.MakeDigestHash(tx);
            var sign = XCrypto.SignHash(this.Account.PrivateKey, digestHash);
            tx.InitiatorSigns = new SignatureInfo[] {
                new SignatureInfo(){
                    PublicKey = this.Account.PublicKey.RawKey,
                    Sign = sign,
                }
            };
            var txid = XDigest.MakeTransactionID(tx);
            tx.Txid = txid;
            var pbtx = XConvert.LocalTxToPbTx(tx);
            var reqTx = new Pb.TxStatus
            {
                Txid = pbtx.Txid,
                Tx = pbtx,
                Bcname = bcname,
            };
            var postRes = client.PostTx(reqTx);
            if (postRes.Header.Error != Pb.XChainErrorEnum.Success)
            {
                Console.WriteLine("post tx failed, err=", (int)postRes.Header.Error);
                response.Error.ErrorCode = Errors.PostError;
                response.Error.ErrorMessage = "Post tx error";
                return response;
            }
            response.Txid = XConvert.ByteArrayToHexString(txid).ToLower();
            return response;
        }
    }
}