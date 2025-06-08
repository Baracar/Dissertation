using System;
using TMPro;
using UnityEngine;

public class UpgradeCard : MonoBehaviour
{
    public TMP_Text text;
    public UpgradeType upgradeType;

    private void Start()
    {
        switch (upgradeType)
        {
            case (UpgradeType.Sniper):
                {
                    text.text = "Sniper upgrade";
                    break;
                }
            case (UpgradeType.Mass):
                {
                    text.text = "Mass destroy upgrade";
                    break;
                }
            default:
                {
                    Debug.LogError("Type of upgrade not choosen");
                    return;
                }
        }
    }

    private void OnMouseDown()
    {
        
    }
    private void OnMouseUpAsButton()
    {
        Shooter shooter = FindAnyObjectByType<Shooter>();
        ShooterUpgrade upgrade;
        switch (upgradeType)
        {
            case (UpgradeType.Sniper):
                {
                    upgrade = new SniperUpgrade(0.2f);
                    break;
                }
            case (UpgradeType.Mass):
                {
                    upgrade = new MassUpgrade(0.01f);
                    break;
                }
            default:
                {
                    Debug.LogError("Type of upgrade not choosen");
                    return;
                }
        }

        shooter.upgrades.Add(upgrade);
    }


}
