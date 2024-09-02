using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Thirdweb;

public class DiamondItemView : MonoBehaviour
{
    [SerializeField] private IAPData _IapData;

    [SerializeField] private Text _TextPrices;
    [SerializeField] private Text _TextValue;

    [SerializeField] private Button _BuyBtn;

    [SerializeField] private Text      _TextSalePercents;
    [SerializeField] private Transform _TransformSale;

    public string Address { get; private set; }
    string gem1AddressSmartContract = "0xCb30856012eE6f94D87B8727B0b522027002aB89";
    string gem2AddressSmartContract = "0x4242f83212004830369E17ED1A98B5CFA689dFEd";
    string gem3AddressSmartContract = "0xB43422851F9C5fC7753826519daB958f4A735c0c";
    string gem4AddressSmartContract = "0x4a6651A0927C9F3af2Ac5cdbba24215206d38CD3";

    private bool IsLocked;

    public void Init ()
    {
        RefreshPrice ();

        _TextValue.text        = _IapData.Value.ToString ();
        _TextSalePercents.text = _IapData.DescriptionSalePercent;

        _TransformSale.gameObject.SetActive (!string.IsNullOrEmpty (_IapData.DescriptionSalePercent));
    }

    private void Start()
    {
        RefreshPrice();
    }

    public void RefreshPrice ()
    {
        _TextPrices.text = _IapData.PriceOffline;
        //if (_IapData.id == IapEnums.IapId.FreePack)
        //{
        //    _TextPrices.text = _IapData.PriceOffline;
        //}
        //else
        //{
        //    //_TextPrices.text = IapManager.Instance.ReturnThePrice (_IapData.id);
        //    _TextPrices.text = _IapData.PriceOffline;
        //}
    }

    public void SetStateUnLock (bool state)
    {
        IsLocked = !state;
     
    }

    public async void DoBuy ()
    {
        this.PlayAudioSound (AudioEnums.SoundId.TapOnButton);

        //Blockchain

        Address = await ThirdwebManager.Instance.SDK.Wallet.GetAddress();

        _BuyBtn.interactable = false;
        _TextPrices.text = "Buying!";

        if (IsLocked)
            return;
        
        switch (_IapData.TypeIap)
        {
            case IapEnums.TypeIap.FreeAds:

                var contractgem1 = ThirdwebManager.Instance.SDK.GetContract(gem1AddressSmartContract);
                await contractgem1.ERC20.ClaimTo(Address, "1");


                //if (this.IsRewardVideoAvailable ())
                //{
                //    this.ExecuteRewardAds (() =>
                //    {
                if (GameActionManager.Instance != null)
                {
                    GameActionManager.Instance.InstanceFxDiamonds(Vector.Vector3Zero,
                                                                   UIGameManager.Instance.GetPositionHubDiamonds(),
                                                                   _IapData.Value);
                }
                else
                {
                    PlayerData.Diamonds += _IapData.Value;
                    PlayerData.SaveDiamonds();

                    this.PostActionEvent(ActionEnums.ActionID.RefreshUIDiamonds);
                }

                PlayerData._LastTimeWatchAdsForFreeDiamonds = Helper.GetUtcTimeString();
                PlayerData.SaveLastTimeWatchAdsForFreeDiamonds();

                if (DiamondManager.Instance != null)
                {
                    DiamondManager.Instance.RefreshTime();
                }

                //    }, null);
                //}
                //else
                //{
                //    this.RefreshRewardVideo ();
                //}

                break;
            case IapEnums.TypeIap.Consumable:

                Debug.Log(_IapData.id);

                if (_IapData.id == IapEnums.IapId.SmallDiamondsPack)
                {
                    var contractgem2 = ThirdwebManager.Instance.SDK.GetContract(gem2AddressSmartContract);
                    await contractgem2.ERC20.ClaimTo(Address, "1");

                } else if (_IapData.id == IapEnums.IapId.MediumDiamondsPack)
                {
                    var contractgem3 = ThirdwebManager.Instance.SDK.GetContract(gem3AddressSmartContract);
                    await contractgem3.ERC20.ClaimTo(Address, "1");

                } else if (_IapData.id == IapEnums.IapId.BigDiamondsPack)
                {
                    var contractgem4 = ThirdwebManager.Instance.SDK.GetContract(gem4AddressSmartContract);
                    await contractgem4.ERC20.ClaimTo(Address, "1");
                }

                if (GameActionManager.Instance != null)
                {
                    GameActionManager.Instance.InstanceFxDiamonds(Vector.Vector3Zero,
                                                                   UIGameManager.Instance.GetPositionHubDiamonds(),
                                                                   _IapData.Value);
                }
                else
                {
                    PlayerData.Diamonds += _IapData.Value;
                    PlayerData.SaveDiamonds();

                    this.PostActionEvent(ActionEnums.ActionID.RefreshUIDiamonds);
                }

                PlayerData._LastTimeWatchAdsForFreeDiamonds = Helper.GetUtcTimeString();
                PlayerData.SaveLastTimeWatchAdsForFreeDiamonds();

                if (DiamondManager.Instance != null)
                {
                    DiamondManager.Instance.RefreshTime();
                }

                Debug.Log("buy");
                break;
            case IapEnums.TypeIap.NonConsumable:
                IapManager.Instance.BuyProductWithID (_IapData.IapId);
                break;
        }

        RefreshPrice();
        _BuyBtn.interactable = true;
    }
}