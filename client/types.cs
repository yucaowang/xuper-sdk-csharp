using System;
using System.Numerics;
using System.Text.Json;

namespace XChainSDK
{
    enum Errors
    {
        Success = 0,
        ConnectFailed,
        UTXONotEnough,
        SelectUTXOError,
        PostError,
        UnknownError,
    }

    class ContactVMType
    {
        public enum Type
        {
            WASM = 0,
            Native,
            EVM,
            XKernel,
        }

        public static string GetNameByType(Type type)
        {
            switch (type)
            {
                case Type.WASM: return "wasm";
                case Type.Native: return "native";
                case Type.EVM: return "evm";
                case Type.XKernel: return "xkernel";
                default:
                    throw new Exception("Unknown type");
            }
        }

        public static Type GetTypeByName(string name)
        {
            switch (name.ToLower())
            {
                case "wasm": return Type.WASM;
                case "native": return Type.Native;
                case "evm": return Type.EVM;
                case "xkernel": return Type.XKernel;
                default:
                    throw new Exception("Unknown name");
            }

        }
    }

    class XChainError
    {
        public Errors ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    class AccountPrivateKey
    {
        private string rawkey;

        public string Curvname { get; set; }
        public BigInteger X { get; set; }
        public BigInteger Y { get; set; }
        public BigInteger D { get; set; }

        public string RawKey
        {
            get
            {
                return rawkey;
            }
        }

        public void ParseJSON(string json)
        {
            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            };

            using (JsonDocument document = JsonDocument.Parse(json, options))
            {
                this.Curvname = document.RootElement.GetProperty("Curvname").GetString();
                var xStr = document.RootElement.GetProperty("X").GetRawText();
                var yStr = document.RootElement.GetProperty("Y").GetRawText();
                var dStr = document.RootElement.GetProperty("D").GetRawText();
                this.X = BigInteger.Parse(xStr);
                this.Y = BigInteger.Parse(yStr);
                this.D = BigInteger.Parse(dStr);
            }
            this.rawkey = json;
        }
    }

    class AccountPublicKey
    {
        private string rawkey;

        public string Curvname { get; set; }
        public BigInteger X { get; set; }
        public BigInteger Y { get; set; }

        public string RawKey
        {
            get
            {
                return rawkey;
            }
        }

        public void ParseJSON(string json)
        {
            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            };

            using (JsonDocument document = JsonDocument.Parse(json, options))
            {
                this.Curvname = document.RootElement.GetProperty("Curvname").GetString();
                var xStr = document.RootElement.GetProperty("X").GetRawText();
                var yStr = document.RootElement.GetProperty("Y").GetRawText();
                this.X = BigInteger.Parse(xStr);
                this.Y = BigInteger.Parse(yStr);
            }
            this.rawkey = json;
        }
    }

    class XCAccount
    {
        public string Address { get; set; }
        public AccountPrivateKey PrivateKey { get; set; }
        public AccountPublicKey PublicKey { get; set; }
    }

    class Response
    {
        public XChainError Error { get; set; }
        public string Txid { get; set; }
        public dynamic Data { get; set; }
    }
}