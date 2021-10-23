using System;
using System.ComponentModel;
using System.Numerics;

using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;


namespace SimpleNFT
{
    [DisplayName("Znadek.SimpleNFTContract")]
    [ManifestExtra("Author", "Mattia Braga")]
    [ManifestExtra("Email", "mattia.braga.91@gmail.com")]
    [ManifestExtra("Description", "Test examples")]
    public class SimpleNFTContract : SmartContract
    {
        static readonly string PreData = "RequstData";
        private static StorageMap ContractStorage => new StorageMap(Storage.CurrentContext, "Storage");
        private static StorageMap ContractMetadata => new StorageMap(Storage.CurrentContext, "Metadata");

        private static Transaction Tx => (Transaction) Runtime.ScriptContainer;

        [DisplayName("ChangeString")]
        public static event Action<string, string> OnChangeString;

        public static bool ChangeString(string tokenId, string str)
        {
            ContractStorage.Put(tokenId, str);
            OnChangeString(tokenId, str);
            return true;
        }

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (!update)
            {
                ContractMetadata.Put("Owner", (ByteString) Tx.Sender);
            }
        }

        public static string GetString(string tokenId)
        {
            return ContractStorage.Get(tokenId);
        }

        public static void UpdateContract(ByteString nefFile, string manifest)
        {
            ByteString owner = ContractMetadata.Get("Owner");
            if (!Tx.Sender.Equals(owner))
            {
                throw new Exception("Only the contract owner can do this");
            }
            ContractManagement.Update(nefFile, manifest, null);
        }


        [DisplayName("Transfer")]
        public static event Action<UInt160, UInt160, BigInteger, ByteString> OnTransfer;

        [Safe]
        [DisplayName("symbol")]
		public static string Symbol() => "SIMPLE";
        
        [Safe]
        [DisplayName("decimals")]
		public static byte Decimals() => 0;
        
        [Safe]
        [DisplayName("totalSupply")]
		public static BigInteger TotalSupply() => 1000;

        [Safe]
		[DisplayName("balanceOf")]
		public static BigInteger BalanceOf(UInt160 owner)
		{
			IsValidAddress(owner, "owner");
			return (BigInteger)ContractStorage[owner];
		}
        
        [Safe]
		[DisplayName("tokensOf")]
		public static Iterator tokensOf(UInt160 owner)
		{
            IsValidAddress(owner, "owner");
            return ContractStorage.Find(owner, FindOptions.KeysOnly | FindOptions.RemovePrefix);
		}

        [Safe]
		[DisplayName("transfer")]
        public static bool Transfer(UInt160 to, ByteString tokenId, object data)
        {
            IsValidAddress(to, "to");
            if (!Runtime.CheckWitness(to)) return false;
            
            ContractMetadata.Delete("Owner");
            ContractMetadata.Put("Owner", (ByteString) to);

            return true;
        }

        public static void Mint(string tokenId, string message)
        {
            ContractStorage.Put(tokenId, message);
            OnChangeString(tokenId, message);
        }

        public static void Burn(ByteString tokenId)
        {
            ContractStorage.Delete(tokenId);
        }

        [Safe]
        [DisplayName("ownerOf")]
		public static UInt160 OwnerOf(ByteString tokenId)
		{
			return (UInt160)ContractMetadata.Get("Owner");
		}

        [Safe]
		[DisplayName("tokens")]
		public static Iterator tokens()
		{
			return ContractStorage.Find(FindOptions.KeysOnly);
		}

        [Safe]
		[DisplayName("properties")]
		public static string Properties(ByteString tokenId)
		{
			return ContractStorage[tokenId];
		}
		public static void Destroy()
		{
			ValidateOwner();
		}

		private static void ValidateOwner()
		{
			if (!Runtime.CheckWitness((UInt160)ContractMetadata.Get("Owner"))) throw new Exception("No authorization");
		}

        private static void IsValidAddress(UInt160 address, string addressDescription = "address")
		{
            if (address is null || !address.IsValid)
                throw new Exception("The argument \"" + addressDescription + "\" is invalid");
		}

        public static void CallFile()
        {
            Runtime.Log("start");
            string url = "https://api.github.com/users/znadek"; // the content is  { "value": "hello world" }
            string filter = "";  // JSONPath format https://github.com/atifaziz/JSONPath
            string callback = "Callback"; // callback method
            object userdata = "userdata"; // arbitrary type
            long gasForResponse = Oracle.MinimumResponseFee;

            Oracle.Request(url, filter, callback, userdata, gasForResponse);
            Runtime.Log("end");
        }

        public static void Callback(string url, byte[] userData, int code, byte[] result)
        {
            Runtime.Log("start callback");
            if (Runtime.CallingScriptHash != Oracle.Hash) throw new Exception("Unauthorized!");
            Storage.Put(Storage.CurrentContext, PreData, result.ToByteString());
        }
// https://github.com/neo-project/examples/blob/master/csharp/Oracle/OracleDemo.cs
        // public static void Callback(string url, string userdata, OracleResponseCode code, string result)
        // {
        //     Runtime.Log("start callback");
        //     //if (ExecutionEngine.CallingScriptHash != Oracle.Hash) throw new Exception("Unauthorized!");
        //     if (code != OracleResponseCode.Success) throw new Exception("Oracle response failure with code " + (byte)code);

        //     object ret = StdLib.JsonDeserialize(result); // [ "hello world" ]
        //     object[] arr = (object[])ret;
        //     string value = (string)arr[0];

        //     Runtime.Log("userdata: " + userdata);
        //     Runtime.Log("response value: " + value);
        // }

        public static string GetRequstData()
        {
            Runtime.Log("GetRequstData");
            return Storage.Get(Storage.CurrentContext, PreData);
        }
    }
}
