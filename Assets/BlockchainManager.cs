using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Thirdweb;
using TMPro;
using UnityEngine.UI;

public class BlockchainManager : MonoBehaviour
{
    public string Address { get; private set; }

    public Button nftButton;
    public Button playButton;

    public TextMeshProUGUI nftButtonText;
    public TextMeshProUGUI playButtonText;

    string nftAddressSmartContract = "0x74bf82a72BB6018983D4987ca891a92d98f99fA5";
    string tokenAddressSmartContract = "0x052Ec11c9782D399EE24Ae1A50d7fE8A2FAD7945";

    private void Start()
    {
        nftButton.gameObject.SetActive(false);
        playButton.gameObject.SetActive(false);
    }

    public async void Login()
    {
        Address = await ThirdwebManager.Instance.SDK.Wallet.GetAddress();
        Debug.Log(Address);
        Contract contract = ThirdwebManager.Instance.SDK.GetContract(nftAddressSmartContract);
        List<NFT> nftList = await contract.ERC721.GetOwned(Address);
        if (nftList.Count == 0)
        {
            nftButton.gameObject.SetActive(true);
        }
        else
        {
            playButton.gameObject.SetActive(true);
        }
    }

    public async void ClaimNFTPass()
    {
        nftButtonText.text = "Claiming...";
        nftButton.interactable = false;
        var contract = ThirdwebManager.Instance.SDK.GetContract(nftAddressSmartContract);
        var result = await contract.ERC721.ClaimTo(Address, 1);
        nftButtonText.text = "Claimed NFT Pass!";
        nftButton.gameObject.SetActive(false);
        playButton.gameObject.SetActive(true);
    }

    public async void ClaimToken()
    {
        playButtonText.text = "Preparing...";
        playButton.interactable = false;
        var contract = ThirdwebManager.Instance.SDK.GetContract(tokenAddressSmartContract);
        var result = await contract.ERC20.ClaimTo(Address, "1");

        SceneManager.LoadScene(1);
        playButtonText.text = "Play";
        playButton.interactable = true;
    }
}
