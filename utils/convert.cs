using System;
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
            if (tx.TxInputs != null && tx.TxInputs.Length > 0)
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

            if (tx.TxOutputs != null && tx.TxOutputs.Length > 0)
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

            if (tx.TxInputsExt != null && tx.TxInputsExt.Length > 0)
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

            if (tx.TxOutputsExt != null && tx.TxOutputsExt.Length > 0)
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

            if (tx.ContractRequests != null && tx.ContractRequests.Length > 0)
            {
                foreach (var cr in tx.ContractRequests)
                {
                    pbtx.ContractRequests.Add(new Pb.InvokeRequest
                    {
                        ModuleName = cr.ModuleName,
                        ContractName = cr.ContractName,
                        MethodName = cr.MethodName,
                        // TODO add contract args
                    });
                }
            }

            pbtx.Initiator = tx.Initiator;
            if (tx.InitiatorSigns != null && tx.InitiatorSigns.Length > 0)
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
            if (tx.AuthRequire != null && tx.AuthRequire.Length > 0)
            {
                foreach (var addr in tx.AuthRequire)
                {
                    pbtx.AuthRequire.Add(addr);
                }
            }
            if (tx.AuthRequireSigns != null && tx.AuthRequireSigns.Length > 0)
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
    }
}