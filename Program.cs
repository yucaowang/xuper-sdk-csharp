using System;
using System.Numerics;
using System.Text;
using Google.Protobuf;
using Pb;

namespace XChainSDK
{
    class Program
    {

        static void Main(string[] args)
        {
            var client = new XChainClient();
            if (!client.Init("./data/keys", "127.0.0.1:37101"))
            {
                Console.WriteLine("Create client failed");
                return;
            }

            // test asn1
            BigInteger r = new BigInteger(190);
            BigInteger s = new BigInteger(1024567);
            var result = XCrypto.Asn1EncodeSign(r, s);
            Console.WriteLine("Encode result=" + XConvert.ByteArrayToHexString(result));

            // test sign
            var data = Encoding.ASCII.GetBytes("123abc");
            var sign = XCrypto.SignHash(client.Account.PrivateKey, data);
            Console.WriteLine("Sign result=" + XConvert.ByteArrayToHexString(sign));

            // test verify sign
            var sign2 = XConvert.HexStringToByteArray("30450221008fb23b0a4e0c1b23cb11517fe25e2eb9ab92c57f62d0d2acf1485a2498ae5dfa02202f480e71c36784af24ca1af1aade44c689fd7a7805a3963e345de3fce71c6b96");
            var valid = XCrypto.VerifySign(client.Account.PublicKey, sign2, data);
            Console.WriteLine("Verify result=" + valid);

            // test get balance
            var balance = client.GetBalance("xuper");
            Console.WriteLine("Account balance=" + balance.ToString());

            // hash test
            var tx = new Transaction();
            tx.Desc = Encoding.ASCII.GetBytes("this is a desc");
            tx.TxInputs = new TxInput[]{
                new TxInput{
                Amount = Encoding.ASCII.GetBytes("888"),
                FromAddr = Encoding.ASCII.GetBytes("dpzuVdosQrF2kmzumhVeFQZa1aYcdgFpN"),
                RefTxid = XConvert.HexStringToByteArray("3027697986fb8f926dc272697e5bc03b8286ef1d1e6604c85b043661b5a8b750"),
                RefOffset = 0,
                FrozenHeight = 0,
            }};
            tx.TxOutputs = new TxOutput[] { new TxOutput
            {
                Amount = Encoding.ASCII.GetBytes("888"),
                ToAddr = Encoding.ASCII.GetBytes("alice"),
            }};
            var digest = XDigest.MakeDigestHash(tx);
            Console.WriteLine("Digest hash=" + BitConverter.ToString(digest).Replace("-", ""));

            // make transaction
            var transRes = client.Transfer("xuper", "alice", new BigInteger(100), "test");
            if (transRes.Error.ErrorCode != Errors.Success)
            {
                Console.WriteLine("Transfer failed, err=" + transRes.Error.ErrorMessage);
            }
            else
            {
                Console.WriteLine("Transfer success, txid=" + transRes.Txid);
            }
        }
    }
}
