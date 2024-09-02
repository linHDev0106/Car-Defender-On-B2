using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using MEC;
using GoogleMobileAds.Common;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor (typeof (AdsManager))]
public class AdsManagerEditor : Editor
{
    public override void OnInspectorGUI ()
    {
        EditorGUILayout.HelpBox ("PLEASE CHECK FILE Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.assets to update App ID before bulding", MessageType.Warning);

        base.OnInspectorGUI ();
    }
}

#endif


public class AdsManager : Singleton<AdsManager>
{
    #region Admobs

    [Header ("CONFIG")] [SerializeField] private string _BannerId;
    [SerializeField] private string interstitialId = string.Empty;
    [SerializeField] private string _RewardVideoId;

    #endregion

    [HideInInspector] public bool IsRewardVideoAvailable;
    [HideInInspector] public bool IsBannerAvailable;

    [Header ("Config")] [SerializeField] private bool IsAutoReloadAgain;

    public System.Action OnCompletedRewardVideo;
    public System.Action OnFailedRewardVideo;

    public System.Action OnFailedFullScreen;
    public System.Action OnCompletedFullScreen;

    private bool IsCompletedTheRewards;
    private bool IsRewardClosed;
    private bool IsRewardValid;

    private RewardedAd rewardedAd;
    private InterstitialAd interstitialAd;
    private BannerView bannerView;

    private bool IsRemoveAds;

    private bool IsWatchedRewardAds;
    private bool IsFirstTimeLoadBanner;

    private CoroutineHandle handleLoadAds;
    private int interstitialAdCount = 0;

    protected override void Awake ()
    {
        base.Awake ();

        MobileAds.SetiOSAppPauseOnBackground(true);
        MobileAds.Initialize((initStatus) =>
        {
            // Callbacks from GoogleMobileAds are not guaranteed to be called on
            // main thread.
            // In this example we use MobileAdsEventExecutor to schedule these calls on
            // the next Update() loop.
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                LoadRewardedAd();
                RefreshRemoveAds();
            });
        });
    }


    public void ShowBanner()
    {
        StartCoroutine(CRLoadAndShowBanner(0.5f));
    }

    public void HideBanner()
    {
        if (bannerView != null)
        {
            bannerView.Hide();
        }
    }

    public void RefreshRemoveAds()
    {
        IsRemoveAds = PlayerData.IsRemoveAds;
        if (IsRemoveAds && bannerView != null)
        {
            bannerView.Hide();
        }
    }

    private IEnumerator<float> _LoadAds ()
    {
        while (IsWatchedRewardAds == false)
        {
            yield return Timing.WaitForOneFrame;
        }

        if (IsWatchedRewardAds)
        {
            IsWatchedRewardAds = false;

            yield return Timing.WaitUntilDone (Timing.RunCoroutine (_ReloadRewardAds ()));
        }
    }

    private IEnumerator<float> _ReloadRewardAds ()
    {
        yield return Timing.WaitForOneFrame;

        IsRewardVideoAvailable = false;

        if (IsRewardClosed && IsRewardValid)
        {
            DoCompletedRewardVideo ();

            IsRewardClosed = false;
            IsRewardValid  = false;
        }
        else
        {
            DoFailedRewardVideo ();

            IsRewardClosed = false;
            IsRewardValid  = false;
        }
    }




    #region Reward Callback


    /// <summary>
    /// Coroutine load and show banner.
    /// </summary>
    /// <param name="delay"></param>
    /// <returns></returns>
    private IEnumerator CRLoadAndShowBanner(float delay)
    {
            yield return new WaitForSeconds(delay);

            // Clean up banner ad before creating a new one.
            if (bannerView != null)
            {
                bannerView.Destroy();
            }

            // Create a banner
            AdSize adSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
#if UNITY_ANDROID
            bannerView = new BannerView(_BannerId, adSize, AdPosition.Bottom);
#elif UNITY_IOS
            bannerView = new BannerView(_BannerId, adSize, AdPosition.Bottom);
#endif
        // Load banner ad.
        AdRequest adRequest = new AdRequest();
            bannerView.LoadAd(adRequest);
    }


    /// <summary>
    /// Load the rewarded ad.
    /// </summary>
    private void LoadRewardedAd()
    {
        // Clean up the old ad before loading a new one.
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }


        // Create our request used to load the ad.
        AdRequest adRequest = new AdRequest();

        //Define the rewarded ad id
        string rewardedAdId = _RewardVideoId;

        //Send the request to load the ad.
        RewardedAd.Load(rewardedAdId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                // If error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    IsRewardVideoAvailable = false;
                    DoFailedRewardVideo();
                    return;
                }
                rewardedAd = ad;
                IsRewardVideoAvailable = true;
                IsRewardClosed = false;
                IsRewardValid = false;


                //Handle rewarded ad failed to show
                rewardedAd.OnAdFullScreenContentFailed += (advalue) =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        LoadRewardedAd();
                    });
                };


                //Handle user close rewarded ad
                rewardedAd.OnAdFullScreenContentClosed += () =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        IsRewardValid = true;
                        IsRewardClosed = true;
                        IsWatchedRewardAds = true;
                        LoadRewardedAd();
                    });
                };

            });
        });
    }


    /// <summary>
    /// Show interstitial ad with given delay time.
    /// </summary>
    /// <param name="delay"></param>
    public void ShowInterstitialAd(float delay)
    {
        interstitialAdCount++;
        if (interstitialAdCount == 5 && !IsRemoveAds)
        {
            interstitialAdCount = 0;
            StartCoroutine(CRShowInterstitialAd(delay));
        }
    }


    /// <summary>
    /// Coroutine show interstitial ad.
    /// </summary>
    /// <param name="delay"></param>
    /// <returns></returns>
    private IEnumerator CRShowInterstitialAd(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
        }
        else
        {
            LoadInterstitialAd();
        }
    }


    /// <summary>
    /// Load the interstitial ad.
    /// </summary>
    private void LoadInterstitialAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        // Create our request used to load the ad.
        AdRequest adRequest = new AdRequest();

        //Send the request to load the ad.
        InterstitialAd.Load(interstitialId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                // If error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    return;
                }
                interstitialAd = ad;

                //Handle interstitial ad closed event
                interstitialAd.OnAdFullScreenContentClosed += () =>
                {
                    MobileAdsEventExecutor.ExecuteInUpdate(() =>
                    {
                        LoadInterstitialAd();
                    });
                };
            });
        });
    }


    #endregion


    private void DoFailedFullScreen ()
    {
        if (OnFailedFullScreen != null)
        {
            OnFailedFullScreen ();
            OnFailedFullScreen = null;
        }
    }

    private void DoCompletedFullScreen ()
    {
        if (OnCompletedFullScreen != null)
        {
            OnCompletedFullScreen ();
            OnCompletedFullScreen = null;
        }
    }

    private void DoFailedRewardVideo ()
    {
        if (OnFailedRewardVideo != null)
        {
            OnFailedRewardVideo ();
            OnFailedRewardVideo = null;
        }

        LogGame.Log ("[Ad Manager] Reward Video Is Failed!");
    }

    private void DoCompletedRewardVideo ()
    {
        if (OnCompletedRewardVideo != null)
        {
            OnCompletedRewardVideo ();
            OnCompletedRewardVideo = null;
        }

        LogGame.Log ("[Ad Manager] Reward Video Is Completed!");
    }

    public void RegisterEvent (System.Action OnCompleted, System.Action OnFailed)
    {
        OnCompletedRewardVideo = OnCompleted;
        OnFailedRewardVideo    = OnFailed;
    }

    public void RegisterEventFullScreen (System.Action OnCompleted, System.Action OnFailed)
    {
        OnFailedFullScreen    = OnFailed;
        OnCompletedFullScreen = OnCompleted;
    }

    public void ShowRewardVideo ()
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            Timing.KillCoroutines(handleLoadAds);
            handleLoadAds = Timing.RunCoroutine(_LoadAds());
            rewardedAd.Show((Reward reward) =>
            {
            });
        }
        else
        {
            DoFailedRewardVideo();
        }
    }
}