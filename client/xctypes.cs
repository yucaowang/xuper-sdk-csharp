using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace XChainSDK
{
    class TxInput
    {
        [JsonProperty(PropertyName = "ref_txid")]
        public byte[] RefTxid { get; set; }

        [JsonProperty(PropertyName = "ref_offset")]
        public int RefOffset { get; set; }

        [JsonProperty(PropertyName = "from_addr")]
        public byte[] FromAddr { get; set; }

        [JsonProperty(PropertyName = "amount")]
        public byte[] Amount { get; set; }

        [JsonProperty(PropertyName = "frozen_height")]
        public Int64 FrozenHeight { get; set; }
    }

    class TxOutput
    {
        [JsonProperty(PropertyName = "amount")]
        public byte[] Amount { get; set; }

        [JsonProperty(PropertyName = "to_addr")]
        public byte[] ToAddr { get; set; }

        [JsonProperty(PropertyName = "frozen_height")]
        public Int64 FrozenHeight { get; set; }
    }

    class TxInputExt
    {
        [JsonProperty(PropertyName = "bucket")]
        public string Bucket { get; set; }

        [JsonProperty(PropertyName = "key")]
        public byte[] Key { get; set; }

        [JsonProperty(PropertyName = "ref_txid")]
        public byte[] RefTxid { get; set; }

        [JsonProperty(PropertyName = "ref_offset")]
        public int RefOffset { get; set; }
    }

    class TxOutputExt
    {
        [JsonProperty(PropertyName = "bucket")]
        public string Bucket { get; set; }

        [JsonProperty(PropertyName = "key")]
        public byte[] Key { get; set; }


        [JsonProperty(PropertyName = "value")]
        public byte[] Value { get; set; }
    }

    enum ResourceType
    {
        CPU = 0,
        MEMORY,
        DISK,
        XFEE // the fee used in kernel contract
    }


    class ResourceLimit
    {
        [JsonProperty(PropertyName = "type")]
        public ResourceType Type;

        [JsonProperty(PropertyName = "limit")]
        public Int64 Limit;
    }

    class InvokeRequest
    {
        [JsonProperty(PropertyName = "module_name")]
        public string ModuleName { get; set; }

        [JsonProperty(PropertyName = "contract_name")]
        [DefaultValue("")]
        public string ContractName { get; set; }

        [JsonProperty(PropertyName = "method_name")]
        public string MethodName { get; set; }

        [JsonProperty(PropertyName = "args")]
        public SortedDictionary<string, byte[]> Args { get; set; }

        [JsonProperty(PropertyName = "resource_limits")]
        public List<ResourceLimit> ResourceLimits { get; set; }

        [JsonProperty(PropertyName = "amount")]
        public string Amount { get; set; }

        public InvokeRequest()
        {
            this.Args = new SortedDictionary<string, byte[]>();
            this.ResourceLimits = new List<ResourceLimit>();
        }
    }

    class SignatureInfo
    {
        public string PublicKey { get; set; }
        public byte[] Sign { get; set; }
    }

    class Transaction
    {
        [JsonProperty(PropertyName = "txid")]
        public byte[] Txid { get; set; }

        [JsonProperty(PropertyName = "blockid")]
        public byte[] Blockid { get; set; }

        [JsonProperty(PropertyName = "tx_inputs")]
        public List<TxInput> TxInputs { get; set; }

        [JsonProperty(PropertyName = "tx_outputs")]
        public List<TxOutput> TxOutputs { get; set; }

        [JsonProperty(PropertyName = "desc")]
        public byte[] Desc { get; set; }

        [JsonProperty(PropertyName = "coinbase")]
        public bool Coinbase { get; set; }

        [JsonProperty(PropertyName = "nonce")]
        public string Nonce { get; set; }

        [JsonProperty(PropertyName = "timestamp")]
        public Int64 Timestamp { get; set; }

        [JsonProperty(PropertyName = "version")]
        public int Version { get; set; }

        [JsonProperty(PropertyName = "autogen")]
        public bool Autogen { get; set; }

        [JsonProperty(PropertyName = "tx_inputs_ext")]
        public List<TxInputExt> TxInputsExt { get; set; }

        [JsonProperty(PropertyName = "tx_outputs_ext")]
        public List<TxOutputExt> TxOutputsExt { get; set; }

        [JsonProperty(PropertyName = "contract_requests")]
        public List<InvokeRequest> ContractRequests { get; set; }

        [JsonProperty(PropertyName = "initiator")]
        public string Initiator { get; set; }

        [JsonProperty(PropertyName = "auth_require")]
        public List<string> AuthRequire { get; set; }

        [JsonProperty(PropertyName = "initiator_signs")]
        public List<SignatureInfo> InitiatorSigns { get; set; }

        [JsonProperty(PropertyName = "auth_require_signs")]
        public List<SignatureInfo> AuthRequireSigns { get; set; }

        [JsonProperty(PropertyName = "received_timestamp")]
        public Int64 ReceivedTimestamp { get; set; }

        public Transaction()
        {
            this.Nonce = "";
            this.Initiator = "";
        }
    }
}