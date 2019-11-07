using System;
using System.Collections.Generic;
using System.Numerics;
using Google.Protobuf;

namespace XChainSDK
{
    class XConvert
    {
        public static string ByteArrayToHexString(byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", string.Empty);
        }

        public static byte[] HexStringToByteArray(string hex)
        {
            var len = hex.Length;
            byte[] result = new byte[len / 2];
            for (int i = 0; i < len; i += 2)
                result[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return result;
        }

        public static byte[] SignedBytesToUnsigned(byte[] signed)
        {
            var len = signed.Length;
            if (signed[len - 1] == 0x0)
            {
                var unsigned = new byte[len - 1];
                Array.Copy(signed, unsigned, len - 1);
                return unsigned;
            }
            return signed;
        }

        public static BigInteger NumberBytesToBigInteger(byte[] number)
        {
            if (number != null)
            {
                Array.Reverse(number, 0, number.Length);
                return new BigInteger(number);
            }
            return new BigInteger(0);
        }

        public static byte[] GetECBytesFromBigInteger(BigInteger val)
        {
            var vBytes = SignedBytesToUnsigned(val.ToByteArray());
            Array.Reverse(vBytes, 0, vBytes.Length);
            return vBytes;
        }

        public static Pb.Transaction LocalTxToPbTx(Transaction tx)
        {
            var pbtx = new Pb.Transaction();
            pbtx.Txid = ByteString.CopyFrom(tx.Txid);
            if (tx.Blockid != null)
            {
                pbtx.Blockid = ByteString.CopyFrom(tx.Blockid);
            }
            if (tx.TxInputs != null && tx.TxInputs.Count > 0)
            {
                foreach (var input in tx.TxInputs)
                {
                    pbtx.TxInputs.Add(new Pb.TxInput
                    {
                        FromAddr = ByteString.CopyFrom(input.FromAddr),
                        Amount = ByteString.CopyFrom(input.Amount),
                        RefOffset = input.RefOffset,
                        RefTxid = ByteString.CopyFrom(input.RefTxid),
                        FrozenHeight = input.FrozenHeight,
                    });
                }
            }

            if (tx.TxOutputs != null && tx.TxOutputs.Count > 0)
            {
                foreach (var output in tx.TxOutputs)
                {
                    pbtx.TxOutputs.Add(new Pb.TxOutput
                    {
                        ToAddr = ByteString.CopyFrom(output.ToAddr),
                        Amount = ByteString.CopyFrom(output.Amount),
                        FrozenHeight = output.FrozenHeight,
                    });
                }
            }
            if (tx.Desc != null)
            {
                pbtx.Desc = ByteString.CopyFrom(tx.Desc);
            }
            pbtx.Coinbase = tx.Coinbase;
            pbtx.Nonce = tx.Nonce;
            pbtx.Timestamp = tx.Timestamp;
            pbtx.Version = tx.Version;
            pbtx.Autogen = tx.Autogen;

            if (tx.TxInputsExt != null && tx.TxInputsExt.Count > 0)
            {
                foreach (var input in tx.TxInputsExt)
                {
                    pbtx.TxInputsExt.Add(new Pb.TxInputExt
                    {
                        Key = ByteString.CopyFrom(input.Key),
                        Bucket = input.Bucket,
                        RefTxid = ByteString.CopyFrom(input.RefTxid),
                        RefOffset = input.RefOffset,
                    });
                }
            }

            if (tx.TxOutputsExt != null && tx.TxOutputsExt.Count > 0)
            {
                foreach (var output in tx.TxOutputsExt)
                {
                    pbtx.TxOutputsExt.Add(new Pb.TxOutputExt
                    {
                        Key = ByteString.CopyFrom(output.Key),
                        Bucket = output.Bucket,
                        Value = ByteString.CopyFrom(output.Value),
                    });
                }
            }

            if (tx.ContractRequests != null && tx.ContractRequests.Count > 0)
            {
                foreach (var cr in tx.ContractRequests)
                {
                    var invokeReq = new Pb.InvokeRequest
                    {
                        ModuleName = cr.ModuleName,
                        ContractName = cr.ContractName,
                        MethodName = cr.MethodName,
                    };
                    foreach (var arg in cr.Args)
                    {
                        invokeReq.Args.Add(arg.Key, ByteString.CopyFrom(arg.Value));
                    }
                    foreach (var limit in cr.ResourceLimits)
                    {
                        invokeReq.ResourceLimits.Add(new Pb.ResourceLimit
                        {
                            Type = (Pb.ResourceType)limit.Type,
                            Limit = limit.Limit,
                        });
                    }
                    pbtx.ContractRequests.Add(invokeReq);
                }
            }

            pbtx.Initiator = tx.Initiator;
            if (tx.InitiatorSigns != null && tx.InitiatorSigns.Count > 0)
            {
                foreach (var sign in tx.InitiatorSigns)
                {
                    pbtx.InitiatorSigns.Add(new Pb.SignatureInfo
                    {
                        PublicKey = sign.PublicKey,
                        Sign = ByteString.CopyFrom(sign.Sign),
                    });
                }
            }
            if (tx.AuthRequire != null && tx.AuthRequire.Count > 0)
            {
                foreach (var addr in tx.AuthRequire)
                {
                    pbtx.AuthRequire.Add(addr);
                }
            }
            if (tx.AuthRequireSigns != null && tx.AuthRequireSigns.Count > 0)
            {
                foreach (var sign in tx.AuthRequireSigns)
                {
                    pbtx.AuthRequireSigns.Add(new Pb.SignatureInfo
                    {
                        PublicKey = sign.PublicKey,
                        Sign = ByteString.CopyFrom(sign.Sign),
                    });
                }
            }

            return pbtx;
        }

        public static Transaction PbTxToLocalTx(Pb.Transaction tx)
        {
            var localTx = new Transaction();
            localTx.Txid = tx.Txid.ToByteArray();
            if (!tx.Blockid.IsEmpty)
            {
                localTx.Blockid = tx.Blockid.ToByteArray();
            }
            if (tx.TxInputs != null && tx.TxInputs.Count > 0)
            {
                localTx.TxInputs = new List<TxInput>();
                foreach (var input in tx.TxInputs)
                {
                    localTx.TxInputs.Add(new TxInput
                    {
                        FromAddr = input.FromAddr.ToByteArray(),
                        Amount = input.Amount.ToByteArray(),
                        RefOffset = input.RefOffset,
                        RefTxid = input.RefTxid.ToByteArray(),
                        FrozenHeight = input.FrozenHeight,
                    });
                }
            }

            if (tx.TxOutputs != null && tx.TxOutputs.Count > 0)
            {
                localTx.TxOutputs = new List<TxOutput>();
                foreach (var output in tx.TxOutputs)
                {
                    localTx.TxOutputs.Add(new TxOutput
                    {
                        ToAddr = output.ToAddr.ToByteArray(),
                        Amount = output.Amount.ToByteArray(),
                        FrozenHeight = output.FrozenHeight,
                    });
                }
            }
            if (tx.Desc != null)
            {
                localTx.Desc = tx.Desc.ToByteArray();
            }
            localTx.Coinbase = tx.Coinbase;
            localTx.Nonce = tx.Nonce;
            localTx.Timestamp = tx.Timestamp;
            localTx.Version = tx.Version;
            localTx.Autogen = tx.Autogen;

            if (tx.TxInputsExt != null && tx.TxInputsExt.Count > 0)
            {
                localTx.TxInputsExt = new List<TxInputExt>();
                foreach (var input in tx.TxInputsExt)
                {
                    localTx.TxInputsExt.Add(new TxInputExt
                    {
                        Key = input.Key.ToByteArray(),
                        Bucket = input.Bucket,
                        RefTxid = input.RefTxid.ToByteArray(),
                        RefOffset = input.RefOffset,
                    });
                }
            }

            if (tx.TxOutputsExt != null && tx.TxOutputsExt.Count > 0)
            {
                localTx.TxOutputsExt = new List<TxOutputExt>();
                foreach (var output in tx.TxOutputsExt)
                {
                    localTx.TxOutputsExt.Add(new TxOutputExt
                    {
                        Key = output.Key.ToByteArray(),
                        Bucket = output.Bucket,
                        Value = output.Value.ToByteArray(),
                    });
                }
            }

            if (tx.ContractRequests != null && tx.ContractRequests.Count > 0)
            {
                localTx.ContractRequests = new List<InvokeRequest>();
                foreach (var cr in tx.ContractRequests)
                {
                    var invokeReq = new InvokeRequest
                    {
                        ModuleName = cr.ModuleName,
                        ContractName = cr.ContractName,
                        MethodName = cr.MethodName,
                    };
                    foreach (var arg in cr.Args)
                    {
                        invokeReq.Args = new SortedDictionary<string, byte[]>();
                        invokeReq.Args.Add(arg.Key, arg.Value.ToByteArray());
                    }
                    foreach (var limit in cr.ResourceLimits)
                    {
                        invokeReq.ResourceLimits = new List<ResourceLimit>();
                        invokeReq.ResourceLimits.Add(new ResourceLimit
                        {
                            Type = (ResourceType)limit.Type,
                            Limit = limit.Limit,
                        });
                    }
                    localTx.ContractRequests.Add(invokeReq);
                }
            }

            localTx.Initiator = tx.Initiator;
            if (tx.InitiatorSigns != null && tx.InitiatorSigns.Count > 0)
            {
                localTx.InitiatorSigns = new List<SignatureInfo>();
                foreach (var sign in tx.InitiatorSigns)
                {
                    localTx.InitiatorSigns.Add(new SignatureInfo
                    {
                        PublicKey = sign.PublicKey,
                        Sign = sign.Sign.ToByteArray(),
                    });
                }
            }
            if (tx.AuthRequire != null && tx.AuthRequire.Count > 0)
            {
                localTx.AuthRequire = new List<string>();
                foreach (var addr in tx.AuthRequire)
                {
                    localTx.AuthRequire.Add(addr);
                }
            }
            if (tx.AuthRequireSigns != null && tx.AuthRequireSigns.Count > 0)
            {
                localTx.AuthRequireSigns = new List<SignatureInfo>();
                foreach (var sign in tx.AuthRequireSigns)
                {
                    localTx.AuthRequireSigns.Add(new SignatureInfo
                    {
                        PublicKey = sign.PublicKey,
                        Sign = sign.Sign.ToByteArray(),
                    });
                }
            }

            return localTx;
        }
    }
}