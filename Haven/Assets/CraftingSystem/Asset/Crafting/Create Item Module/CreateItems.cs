using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreateItems : MonoBehaviour
{
    /*Singleton*/
    private static CreateItems m_oInstance = null;
    private int m_nCounter = 0;
    private static readonly object m_oPadLock = new object();

    public static CreateItems Instance
    {
        get
        {
            lock (m_oPadLock)
            {
                if (m_oInstance == null)
                {
                    m_oInstance = new CreateItems();
                }
                return m_oInstance;
            }

        }
    }

    private CreateItems()
    {
        m_nCounter = 1;
    }
    /*Koniec Singletona*/

    public void CreateNewItem(Recipe chosenRecipe, Metal[] chosenMaterials)
    {
        if (chosenRecipe.GetType().Equals(typeof(WeaponRecipe)))
        {
            CreateNewWeapon((WeaponRecipe)chosenRecipe, chosenMaterials);
        }else if (chosenRecipe.GetType().Equals(typeof(TwoHandedWeaponRecipe)))
        {
            CreateNewTwoHandedWeapon((TwoHandedWeaponRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(ArmorRecipe)))
        {
            CreateNewArmor((ArmorRecipe)chosenRecipe, chosenMaterials);
        }else if (chosenRecipe.GetType().Equals(typeof(LegsRecipe)))
        {
            CreateNewLegs((LegsRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(HelmetRecipe)))
        {
            CreateNewHelmet((HelmetRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(BootsRecipe)))
        {
            CreateNewBoots((BootsRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(GlovesRecipe)))
        {
            CreateNewGloves((GlovesRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(NecklaceRecipe)))
        {
            CreateNewNecklace((NecklaceRecipe)chosenRecipe, chosenMaterials);

        }
        else if (chosenRecipe.GetType().Equals(typeof(RingRecipe)))
        {
            CreateNewRing((RingRecipe)chosenRecipe, chosenMaterials);
        }
    }

    public void CreateNewItem(Recipe chosenRecipe, Wood[] chosenMaterials)
    {
        if (chosenRecipe.GetType().Equals(typeof(WeaponRecipe)))
        {
            CreateNewWeapon((WeaponRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(TwoHandedWeaponRecipe)))
        {
            CreateNewTwoHandedWeapon((TwoHandedWeaponRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(NecklaceRecipe)))
        {
            CreateNewNecklace((NecklaceRecipe)chosenRecipe, chosenMaterials);

        }
        else if (chosenRecipe.GetType().Equals(typeof(RingRecipe)))
        {
            CreateNewRing((RingRecipe)chosenRecipe, chosenMaterials);
        }
    }

    public void CreateNewItem(Recipe chosenRecipe, Rune[] chosenMaterials)
    {
        if (chosenRecipe.GetType().Equals(typeof(WandRecipe)))
        {
            CreateNewWand((WandRecipe)chosenRecipe, chosenMaterials);
        }
    }

    public void CreateNewItem(Recipe chosenRecipe, Cloth[] chosenMaterials)
    {
        if (chosenRecipe.GetType().Equals(typeof(ArmorRecipe)))
        {
            CreateNewArmor((ArmorRecipe)chosenRecipe, chosenMaterials);
        }else if (chosenRecipe.GetType().Equals(typeof(LegsRecipe)))
        {
            CreateNewLegs((LegsRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(HelmetRecipe)))
        {
            CreateNewHelmet((HelmetRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(BootsRecipe)))
        {
            CreateNewBoots((BootsRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(GlovesRecipe)))
        {
            CreateNewGloves((GlovesRecipe)chosenRecipe, chosenMaterials);
        }
    }

    public void CreateNewItem(Recipe chosenRecipe, Leather[] chosenMaterials)
    {
        if (chosenRecipe.GetType().Equals(typeof(ArmorRecipe)))
        {
            CreateNewArmor((ArmorRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(LegsRecipe)))
        {
            CreateNewLegs((LegsRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(HelmetRecipe)))
        {
            CreateNewHelmet((HelmetRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(BootsRecipe)))
        {
            CreateNewBoots((BootsRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(GlovesRecipe)))
        {
            CreateNewGloves((GlovesRecipe)chosenRecipe, chosenMaterials);
        }
    }

    public void CreateNewItem(Recipe chosenRecipe, Liquid[] chosenMaterials)
    {
        if (chosenRecipe.GetType().Equals(typeof(PotionRecipe)))
        {
            CreateNewPotion((PotionRecipe)chosenRecipe, chosenMaterials);
        }
    }

    public void CreateNewItem(Recipe chosenRecipe, Stone[] chosenMaterials)
    {
        if (chosenRecipe.GetType().Equals(typeof(WeaponRecipe)))
        {
            CreateNewWeapon((WeaponRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(TwoHandedWeaponRecipe)))
        {
            CreateNewTwoHandedWeapon((TwoHandedWeaponRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(ArmorRecipe)))
        {
            CreateNewArmor((ArmorRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(LegsRecipe)))
        {
            CreateNewLegs((LegsRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(HelmetRecipe)))
        {
            CreateNewHelmet((HelmetRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(BootsRecipe)))
        {
            CreateNewBoots((BootsRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(GlovesRecipe)))
        {
            CreateNewGloves((GlovesRecipe)chosenRecipe, chosenMaterials);
        }
        else if (chosenRecipe.GetType().Equals(typeof(NecklaceRecipe)))
        {
            CreateNewNecklace((NecklaceRecipe)chosenRecipe, chosenMaterials);

        }
        else if (chosenRecipe.GetType().Equals(typeof(RingRecipe)))
        {
            CreateNewRing((RingRecipe)chosenRecipe, chosenMaterials);
        }
    }



    public void CreateNewItem(Recipe chosenRecipe)
    {
        if (chosenRecipe.GetType().Equals(typeof(WeaponRecipe)))
        {
            CreateNewWeapon((WeaponRecipe)chosenRecipe);
        }else if (chosenRecipe.GetType().Equals(typeof(ArmorRecipe)))
        {
            CreateNewArmor((ArmorRecipe)chosenRecipe);
        }else if (chosenRecipe.GetType().Equals(typeof(PartRecipe)))
        {
            CreateNewPart((PartRecipe)chosenRecipe);
        }else if (chosenRecipe.GetType().Equals(typeof(WandRecipe))){
            CreateNewWand((WandRecipe)chosenRecipe);
        }

    }

    public void CreateNewPart(PartRecipe chosenRecipe)
    {
        Part createdPart = new Part(chosenRecipe.name);
        createdPart.color = chosenRecipe.defaultColor;
        createdPart.description = chosenRecipe.description;
        createdPart.lvl = chosenRecipe.lvl;
        createdPart.image = chosenRecipe.image;
        createdPart.exp = chosenRecipe.exp;
        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenRecipe.time, createdPart);
    }

    public void CreateNewWeapon(WeaponRecipe chosenRecipe)
    {
        Weapon createdWeapon = new Weapon(chosenRecipe.name);
        createdWeapon.attack = chosenRecipe.multiplier_physical_attack;
        createdWeapon.attackSpeed = chosenRecipe.multiplier_speed_attack;
        createdWeapon.magicAttack = chosenRecipe.multiplier_magic_attack;
        createdWeapon.criticalHits = chosenRecipe.multiplier_crytical_damage;
        createdWeapon.defence = chosenRecipe.multiplier_defence;
        createdWeapon.color = chosenRecipe.defaultColor;
        createdWeapon.description = chosenRecipe.description;
        createdWeapon.lvl = chosenRecipe.lvl;
        createdWeapon.image = chosenRecipe.image;
        createdWeapon.exp = chosenRecipe.exp;
        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenRecipe.time, createdWeapon);
    }

    public void CreateNewWand(WandRecipe chosenRecipe)
    {
        Wand createdWeapon = new Wand(chosenRecipe.name);
        createdWeapon.description = chosenRecipe.description;
        createdWeapon.image = chosenRecipe.image;
        createdWeapon.windAttack = chosenRecipe.multiplier_windAttack;
        createdWeapon.waterAttack = chosenRecipe.multiplier_waterAttack;
        createdWeapon.earthAttack = chosenRecipe.multiplier_earthAttack;
        createdWeapon.fireAttack = chosenRecipe.multiplier_fireAttack;
        createdWeapon.lvl = chosenRecipe.lvl;
        createdWeapon.color = chosenRecipe.defaultColor;
        createdWeapon.exp = chosenRecipe.exp;
        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenRecipe.time, createdWeapon);
    }

    public void CreateNewArmor(ArmorRecipe chosenRecipe)
    {
        Armor createdNewArmor = new Armor( chosenRecipe.name);
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence = chosenRecipe.multiplier_magicDefence;
        createdNewArmor.frostDefence = chosenRecipe.multiplier_frostDefence;
        createdNewArmor.windDefence = chosenRecipe.multiplier_windDefence;
        createdNewArmor.earthDefence = chosenRecipe.multiplier_earthDefence;
        createdNewArmor.fireDefence = chosenRecipe.multiplier_fireDefence;
        createdNewArmor.defence = chosenRecipe.multiplier_defence;
        createdNewArmor.lvl = chosenRecipe.lvl;
        createdNewArmor.color = chosenRecipe.defaultColor;
        createdNewArmor.exp = chosenRecipe.exp;


        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenRecipe.time, createdNewArmor);
    }

    //Cloth
    public void CreateNewArmor(ArmorRecipe chosenRecipe, Cloth[] chosenMaterials)
    {
        Armor createdNewArmor = new Armor("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].thickness;
            createdNewArmor.windDefence += chosenMaterials[i].density;
            createdNewArmor.frostDefence += chosenMaterials[i].thickness;
            createdNewArmor.frostDefence += chosenMaterials[i].flexibility;
            createdNewArmor.magicDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.fireDefence += chosenMaterials[i].density;
            createdNewArmor.earthDefence += chosenMaterials[i].magicEfficiency;

            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.defence += chosenMaterials[i].thickness;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / (chosenMaterials.Length * 2);
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 3);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewLegs(LegsRecipe chosenRecipe, Cloth[] chosenMaterials)
    {
        Legs createdNewArmor = new Legs("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].thickness;
            createdNewArmor.windDefence += chosenMaterials[i].density;
            createdNewArmor.frostDefence += chosenMaterials[i].thickness;
            createdNewArmor.frostDefence += chosenMaterials[i].flexibility;
            createdNewArmor.magicDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.fireDefence += chosenMaterials[i].density;
            createdNewArmor.earthDefence += chosenMaterials[i].magicEfficiency;

            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.defence += chosenMaterials[i].thickness;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / (chosenMaterials.Length * 2);
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 3);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewHelmet(HelmetRecipe chosenRecipe, Cloth[] chosenMaterials)
    {
        Helmet createdNewArmor = new Helmet("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].thickness;
            createdNewArmor.windDefence += chosenMaterials[i].density;
            createdNewArmor.frostDefence += chosenMaterials[i].thickness;
            createdNewArmor.frostDefence += chosenMaterials[i].flexibility;
            createdNewArmor.magicDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.fireDefence += chosenMaterials[i].density;
            createdNewArmor.earthDefence += chosenMaterials[i].magicEfficiency;

            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.defence += chosenMaterials[i].thickness;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / (chosenMaterials.Length * 2);
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 3);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewBoots(BootsRecipe chosenRecipe, Cloth[] chosenMaterials)
    {
        Boots createdNewArmor = new Boots("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].thickness;
            createdNewArmor.windDefence += chosenMaterials[i].density;
            createdNewArmor.frostDefence += chosenMaterials[i].thickness;
            createdNewArmor.frostDefence += chosenMaterials[i].flexibility;
            createdNewArmor.magicDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.fireDefence += chosenMaterials[i].density;
            createdNewArmor.earthDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.speed += chosenMaterials[i].flexibility;

            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.defence += chosenMaterials[i].thickness;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdNewArmor.speed *= chosenRecipe.multiplier_speed / chosenMaterials.Length;
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / (chosenMaterials.Length * 2);
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 3);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewGloves(GlovesRecipe chosenRecipe, Cloth[] chosenMaterials)
    {
        Gloves createdNewArmor = new Gloves("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].thickness;
            createdNewArmor.windDefence += chosenMaterials[i].density;
            createdNewArmor.frostDefence += chosenMaterials[i].thickness;
            createdNewArmor.frostDefence += chosenMaterials[i].flexibility;
            createdNewArmor.magicDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.fireDefence += chosenMaterials[i].density;
            createdNewArmor.earthDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.speedAtack += chosenMaterials[i].flexibility;

            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.defence += chosenMaterials[i].thickness;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdNewArmor.speedAtack *= chosenRecipe.multiplier_speedAtack / chosenMaterials.Length;
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / (chosenMaterials.Length * 2);
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 3);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }

    //Leather
    public void CreateNewArmor(ArmorRecipe chosenRecipe, Leather[] chosenMaterials)
    {
        Armor createdNewArmor = new Armor("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].temperatureConductivity;
            createdNewArmor.windDefence += chosenMaterials[i].density;
            createdNewArmor.frostDefence += chosenMaterials[i].temperatureConductivity;
            createdNewArmor.frostDefence += chosenMaterials[i].flexibility;
            createdNewArmor.magicDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.fireDefence += chosenMaterials[i].density;
            createdNewArmor.earthDefence += chosenMaterials[i].magicEfficiency;

            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / (chosenMaterials.Length * 2);
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewLegs(LegsRecipe chosenRecipe, Leather[] chosenMaterials)
    {
        Legs createdNewArmor = new Legs("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].temperatureConductivity;
            createdNewArmor.windDefence += chosenMaterials[i].density;
            createdNewArmor.frostDefence += chosenMaterials[i].temperatureConductivity;
            createdNewArmor.frostDefence += chosenMaterials[i].flexibility;
            createdNewArmor.magicDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.fireDefence += chosenMaterials[i].density;
            createdNewArmor.earthDefence += chosenMaterials[i].magicEfficiency;

            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / (chosenMaterials.Length * 2);
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewHelmet(HelmetRecipe chosenRecipe, Leather[] chosenMaterials)
    {
        Helmet createdNewArmor = new Helmet("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].temperatureConductivity;
            createdNewArmor.windDefence += chosenMaterials[i].density;
            createdNewArmor.frostDefence += chosenMaterials[i].temperatureConductivity;
            createdNewArmor.frostDefence += chosenMaterials[i].flexibility;
            createdNewArmor.magicDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.fireDefence += chosenMaterials[i].density;
            createdNewArmor.earthDefence += chosenMaterials[i].magicEfficiency;

            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / (chosenMaterials.Length * 2);
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewBoots(BootsRecipe chosenRecipe, Leather[] chosenMaterials)
    {
        Boots createdNewArmor = new Boots("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].temperatureConductivity;
            createdNewArmor.windDefence += chosenMaterials[i].density;
            createdNewArmor.frostDefence += chosenMaterials[i].temperatureConductivity;
            createdNewArmor.frostDefence += chosenMaterials[i].flexibility;
            createdNewArmor.magicDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.fireDefence += chosenMaterials[i].density;
            createdNewArmor.earthDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.speed += chosenMaterials[i].flexibility;

            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdNewArmor.speed *= chosenRecipe.multiplier_speed / chosenMaterials.Length;
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / (chosenMaterials.Length * 2);
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewGloves(GlovesRecipe chosenRecipe, Leather[] chosenMaterials)
    {
        Gloves createdNewArmor = new Gloves("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].temperatureConductivity;
            createdNewArmor.windDefence += chosenMaterials[i].density;
            createdNewArmor.frostDefence += chosenMaterials[i].temperatureConductivity;
            createdNewArmor.frostDefence += chosenMaterials[i].flexibility;
            createdNewArmor.magicDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.fireDefence += chosenMaterials[i].density;
            createdNewArmor.earthDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.speedAtack += chosenMaterials[i].flexibility;

            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.defence += chosenMaterials[i].temperatureConductivity;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdNewArmor.speedAtack *= chosenRecipe.multiplier_speedAtack / chosenMaterials.Length;
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / (chosenMaterials.Length * 2);
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }

    //Liquid
    public void CreateNewPotion(PotionRecipe chosenRecipe, Liquid[] chosenMaterials)
    {
        Potion createdNewArmor = new Potion("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.speed = chosenMaterials[i].speedInpact;
            createdNewArmor.posionous = chosenMaterials[i].poisonous;
            createdNewArmor.HPrecovery = chosenMaterials[i].healtAbility;
            createdNewArmor.MPrecovery = chosenMaterials[i].density;
            createdNewArmor.resistance = chosenMaterials[i].resistance;
            createdNewArmor.magicResistance = chosenMaterials[i].magicEfficiency;
            createdNewArmor.timeAction = chosenMaterials[i].getTimeParameter();
             createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.speed *= chosenRecipe.multiplier_speed / chosenMaterials.Length;
        createdNewArmor.posionous *= chosenRecipe.multiplier_posionous / chosenMaterials.Length;
        createdNewArmor.HPrecovery *= chosenRecipe.multiplier_HPrecovery / chosenMaterials.Length;
        createdNewArmor.MPrecovery *= chosenRecipe.multiplier_MPrecovery / chosenMaterials.Length;
        createdNewArmor.resistance *= chosenRecipe.multiplier_resistance / chosenMaterials.Length;
        createdNewArmor.magicResistance *= chosenRecipe.multiplier_magicResistance / chosenMaterials.Length;
        createdNewArmor.timeAction *= chosenRecipe.multiplier_timeAction / chosenMaterials.Length;
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }

    //Metal
    public void CreateNewWeapon(WeaponRecipe chosenRecipe, Metal[] chosenMaterials)
    {
        Weapon createdWeapon = new Weapon("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdWeapon.attack += chosenMaterials[i].hardness;
            createdWeapon.attackSpeed += chosenMaterials[i].density;
            createdWeapon.magicAttack += chosenMaterials[i].thermalConductivity;
            createdWeapon.magicAttack += chosenMaterials[i].electricalConductivity;
            createdWeapon.criticalHits += chosenMaterials[i].electricalConductivity;
            createdWeapon.defence += chosenMaterials[i].hardness;
            createdWeapon.defence += chosenMaterials[i].density;
            createdWeapon.color += chosenMaterials[i].color;
        }

        createdWeapon.exp = chosenRecipe.exp;
        createdWeapon.description = chosenRecipe.description;
        createdWeapon.image = chosenRecipe.image;
        createdWeapon.attack *= chosenRecipe.multiplier_physical_attack / chosenMaterials.Length;
        createdWeapon.attackSpeed *= chosenRecipe.multiplier_speed_attack / chosenMaterials.Length;
        createdWeapon.magicAttack *= chosenRecipe.multiplier_magic_attack / (chosenMaterials.Length * 2);
        createdWeapon.criticalHits *= chosenRecipe.multiplier_crytical_damage / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdWeapon.color /= chosenMaterials.Length;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdWeapon);

    }
    public void CreateNewTwoHandedWeapon(TwoHandedWeaponRecipe chosenRecipe, Metal[] chosenMaterials)
    {
        TwoHandedWeapon createdWeapon = new TwoHandedWeapon("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdWeapon.attack += chosenMaterials[i].hardness;
            createdWeapon.attackSpeed += chosenMaterials[i].density;
            createdWeapon.magicAttack += chosenMaterials[i].thermalConductivity;
            createdWeapon.magicAttack += chosenMaterials[i].electricalConductivity;
            createdWeapon.criticalHits += chosenMaterials[i].electricalConductivity;
            createdWeapon.defence += chosenMaterials[i].hardness;
            createdWeapon.defence += chosenMaterials[i].density;
            createdWeapon.color += chosenMaterials[i].color;
        }

        createdWeapon.exp = chosenRecipe.exp;
        createdWeapon.description = chosenRecipe.description;
        createdWeapon.image = chosenRecipe.image;
        createdWeapon.attack *= chosenRecipe.multiplier_physical_attack / chosenMaterials.Length;
        createdWeapon.attackSpeed *= chosenRecipe.multiplier_speed_attack / chosenMaterials.Length;
        createdWeapon.magicAttack *= chosenRecipe.multiplier_magic_attack / (chosenMaterials.Length * 2);
        createdWeapon.criticalHits *= chosenRecipe.multiplier_crytical_damage / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdWeapon.color /= chosenMaterials.Length;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdWeapon);

    }
    public void CreateNewArmor(ArmorRecipe chosenRecipe, Metal[] chosenMaterials)
    {
        Armor createdNewArmor = new Armor("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].thermalInsulation;
            createdNewArmor.windDefence += chosenMaterials[i].thermalConductivity;
            createdNewArmor.frostDefence += chosenMaterials[i].thermalInsulation;
            createdNewArmor.magicDefence += chosenMaterials[i].electricalConductivity;
            createdNewArmor.fireDefence += chosenMaterials[i].thermalConductivity;
            createdNewArmor.earthDefence += chosenMaterials[i].meltingTemperature;

            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / chosenMaterials.Length;
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewLegs(LegsRecipe chosenRecipe, Metal[] chosenMaterials)
    {
        Legs createdNewArmor = new Legs("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].thermalInsulation;
            createdNewArmor.windDefence += chosenMaterials[i].thermalConductivity;
            createdNewArmor.frostDefence += chosenMaterials[i].thermalInsulation;
            createdNewArmor.magicDefence += chosenMaterials[i].electricalConductivity;
            createdNewArmor.fireDefence += chosenMaterials[i].thermalConductivity;
            createdNewArmor.earthDefence += chosenMaterials[i].meltingTemperature;

            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / chosenMaterials.Length;
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewHelmet(HelmetRecipe chosenRecipe, Metal[] chosenMaterials)
    {
        Helmet createdNewArmor = new Helmet("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].thermalInsulation;
            createdNewArmor.windDefence += chosenMaterials[i].thermalConductivity;
            createdNewArmor.frostDefence += chosenMaterials[i].thermalInsulation;
            createdNewArmor.magicDefence += chosenMaterials[i].electricalConductivity;
            createdNewArmor.fireDefence += chosenMaterials[i].thermalConductivity;
            createdNewArmor.earthDefence += chosenMaterials[i].meltingTemperature;

            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / chosenMaterials.Length;
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewBoots(BootsRecipe chosenRecipe, Metal[] chosenMaterials)
    {
        Boots createdNewArmor = new Boots("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].thermalInsulation;
            createdNewArmor.windDefence += chosenMaterials[i].thermalConductivity;
            createdNewArmor.frostDefence += chosenMaterials[i].thermalInsulation;
            createdNewArmor.magicDefence += chosenMaterials[i].electricalConductivity;
            createdNewArmor.fireDefence += chosenMaterials[i].thermalConductivity;
            createdNewArmor.earthDefence += chosenMaterials[i].meltingTemperature;
            createdNewArmor.speed += chosenMaterials[i].electricalConductivity;

            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / chosenMaterials.Length;
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdNewArmor.speed *= chosenRecipe.multiplier_speed / chosenMaterials.Length;
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewGloves(GlovesRecipe chosenRecipe, Metal[] chosenMaterials)
    {
        Gloves createdNewArmor = new Gloves("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].thermalInsulation;
            createdNewArmor.windDefence += chosenMaterials[i].thermalConductivity;
            createdNewArmor.frostDefence += chosenMaterials[i].thermalInsulation;
            createdNewArmor.magicDefence += chosenMaterials[i].electricalConductivity;
            createdNewArmor.fireDefence += chosenMaterials[i].thermalConductivity;
            createdNewArmor.earthDefence += chosenMaterials[i].meltingTemperature;
            createdNewArmor.speedAtack += chosenMaterials[i].electricalConductivity;


            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / chosenMaterials.Length;
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.speedAtack *= chosenRecipe.multiplier_speedAtack / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewNecklace(NecklaceRecipe chosenRecipe, Metal[] chosenMaterials)
    {
        Necklace createdWeapon = new Necklace("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdWeapon.attack += chosenMaterials[i].hardness;
            createdWeapon.attackSpeed += chosenMaterials[i].electricalConductivity;
            createdWeapon.magicAttack += chosenMaterials[i].electricalConductivity;
            createdWeapon.magicAttack += chosenMaterials[i].thermalConductivity;
            createdWeapon.criticalHits += chosenMaterials[i].electricalConductivity;
            createdWeapon.defence += chosenMaterials[i].hardness;

            createdWeapon.earthAttack += chosenMaterials[i].electricalConductivity;
            createdWeapon.earthAttack += chosenMaterials[i].hardness;
            createdWeapon.fireAttack += chosenMaterials[i].thermalConductivity;
            createdWeapon.waterAttack += chosenMaterials[i].electricalConductivity;
            createdWeapon.windAttack += chosenMaterials[i].electricalConductivity;

            createdWeapon.magicDefence += chosenMaterials[i].electricalConductivity;
            createdWeapon.fireDefence += chosenMaterials[i].thermalConductivity;
            createdWeapon.earthDefence += chosenMaterials[i].hardness;
            createdWeapon.frostDefence += chosenMaterials[i].electricalConductivity;
            createdWeapon.windDefence += chosenMaterials[i].electricalConductivity;
            createdWeapon.color += chosenMaterials[i].color;
        }
        createdWeapon.exp = chosenRecipe.exp;
        createdWeapon.description = chosenRecipe.description;
        createdWeapon.image = chosenRecipe.image;
        createdWeapon.attack *= chosenRecipe.multiplier_attack;
        createdWeapon.attackSpeed *= chosenRecipe.multiplier_attackSpeed / chosenMaterials.Length;
        createdWeapon.magicAttack *= chosenRecipe.multiplier_magicAttack / (chosenMaterials.Length * 2);
        createdWeapon.criticalHits *= chosenRecipe.multiplier_criticalHits / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / chosenMaterials.Length;

        createdWeapon.earthAttack *= chosenRecipe.multiplier_earthAttack / (chosenMaterials.Length * 2);
        createdWeapon.fireAttack *= chosenRecipe.multiplier_fireAttack / chosenMaterials.Length;
        createdWeapon.waterAttack *= chosenRecipe.multiplier_waterAttack / chosenMaterials.Length;
        createdWeapon.windAttack *= chosenRecipe.multiplier_windAttack / chosenMaterials.Length;

        createdWeapon.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdWeapon.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdWeapon.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdWeapon.frostDefence *= chosenRecipe.multiplier_frostDefence / chosenMaterials.Length;
        createdWeapon.windDefence *= chosenRecipe.multiplier_windDefence / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdWeapon.color /= chosenMaterials.Length;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdWeapon);
    }
    public void CreateNewRing(RingRecipe chosenRecipe, Metal[] chosenMaterials)
    {
        Ring createdWeapon = new Ring("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdWeapon.attack += chosenMaterials[i].hardness;
            createdWeapon.attackSpeed += chosenMaterials[i].electricalConductivity;
            createdWeapon.magicAttack += chosenMaterials[i].electricalConductivity;
            createdWeapon.magicAttack += chosenMaterials[i].thermalConductivity;
            createdWeapon.criticalHits += chosenMaterials[i].electricalConductivity;
            createdWeapon.defence += chosenMaterials[i].hardness;

            createdWeapon.earthAttack += chosenMaterials[i].electricalConductivity;
            createdWeapon.earthAttack += chosenMaterials[i].hardness;
            createdWeapon.fireAttack += chosenMaterials[i].thermalConductivity;
            createdWeapon.waterAttack += chosenMaterials[i].electricalConductivity;
            createdWeapon.windAttack += chosenMaterials[i].electricalConductivity;

            createdWeapon.magicDefence += chosenMaterials[i].electricalConductivity;
            createdWeapon.fireDefence += chosenMaterials[i].thermalConductivity;
            createdWeapon.earthDefence += chosenMaterials[i].hardness;
            createdWeapon.frostDefence += chosenMaterials[i].electricalConductivity;
            createdWeapon.windDefence += chosenMaterials[i].electricalConductivity;
            createdWeapon.color += chosenMaterials[i].color;
        }
        createdWeapon.exp = chosenRecipe.exp;
        createdWeapon.description = chosenRecipe.description;
        createdWeapon.image = chosenRecipe.image;
        createdWeapon.attack *= chosenRecipe.multiplier_attack;
        createdWeapon.attackSpeed *= chosenRecipe.multiplier_attackSpeed / chosenMaterials.Length;
        createdWeapon.magicAttack *= chosenRecipe.multiplier_magicAttack / (chosenMaterials.Length * 2);
        createdWeapon.criticalHits *= chosenRecipe.multiplier_criticalHits / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / chosenMaterials.Length;

        createdWeapon.earthAttack *= chosenRecipe.multiplier_earthAttack / (chosenMaterials.Length * 2);
        createdWeapon.fireAttack *= chosenRecipe.multiplier_fireAttack / chosenMaterials.Length;
        createdWeapon.waterAttack *= chosenRecipe.multiplier_waterAttack / chosenMaterials.Length;
        createdWeapon.windAttack *= chosenRecipe.multiplier_windAttack / chosenMaterials.Length;

        createdWeapon.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdWeapon.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdWeapon.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdWeapon.frostDefence *= chosenRecipe.multiplier_frostDefence / chosenMaterials.Length;
        createdWeapon.windDefence *= chosenRecipe.multiplier_windDefence / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdWeapon.color /= chosenMaterials.Length;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdWeapon);
    }

    //Rune
    public void CreateNewWand(WandRecipe chosenRecipe, Rune[] chosenMaterials)
    {
        Wand createdWeapon = new Wand("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdWeapon.fireAttack += chosenMaterials[i].fire;
            createdWeapon.windAttack += chosenMaterials[i].wind;
            createdWeapon.earthAttack += chosenMaterials[i].earth;
            createdWeapon.waterAttack += chosenMaterials[i].water;
            createdWeapon.color += chosenMaterials[i].color;
        }

        createdWeapon.exp = chosenRecipe.exp;
        createdWeapon.description = chosenRecipe.description;
        createdWeapon.image = chosenRecipe.image;
        createdWeapon.windAttack *= chosenRecipe.multiplier_windAttack / chosenMaterials.Length;
        createdWeapon.waterAttack *= chosenRecipe.multiplier_waterAttack / (chosenMaterials.Length);
        createdWeapon.earthAttack *= chosenRecipe.multiplier_earthAttack / chosenMaterials.Length;
        createdWeapon.fireAttack *= chosenRecipe.multiplier_fireAttack / (chosenMaterials.Length );
        createdWeapon.color /= chosenMaterials.Length;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdWeapon);
    }

    //Stone
    public void CreateNewWeapon(WeaponRecipe chosenRecipe, Stone[] chosenMaterials)
    {
        Weapon createdWeapon = new Weapon("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdWeapon.attack += chosenMaterials[i].hardness;
            createdWeapon.attackSpeed += chosenMaterials[i].density;
            createdWeapon.magicAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.criticalHits += chosenMaterials[i].flexibility;
            createdWeapon.defence += chosenMaterials[i].hardness;
            createdWeapon.defence += chosenMaterials[i].density;
            createdWeapon.defence += chosenMaterials[i].resistance;
            createdWeapon.color += chosenMaterials[i].color;
        }

        createdWeapon.exp = chosenRecipe.exp;
        createdWeapon.description = chosenRecipe.description;
        createdWeapon.image = chosenRecipe.image;
        createdWeapon.attack *= chosenRecipe.multiplier_physical_attack / chosenMaterials.Length;
        createdWeapon.attackSpeed *= chosenRecipe.multiplier_speed_attack / chosenMaterials.Length;
        createdWeapon.magicAttack *= chosenRecipe.multiplier_magic_attack / (chosenMaterials.Length * 1);
        createdWeapon.criticalHits *= chosenRecipe.multiplier_crytical_damage / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 3);
        createdWeapon.color /= chosenMaterials.Length;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdWeapon);

    }
    public void CreateNewTwoHandedWeapon(TwoHandedWeaponRecipe chosenRecipe, Stone[] chosenMaterials)
    {
        TwoHandedWeapon createdWeapon = new TwoHandedWeapon("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdWeapon.attack += chosenMaterials[i].hardness;
            createdWeapon.attackSpeed += chosenMaterials[i].density;
            createdWeapon.magicAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.criticalHits += chosenMaterials[i].flexibility;
            createdWeapon.defence += chosenMaterials[i].hardness;
            createdWeapon.defence += chosenMaterials[i].density;
            createdWeapon.defence += chosenMaterials[i].resistance;
            createdWeapon.color += chosenMaterials[i].color;
        }

        createdWeapon.exp = chosenRecipe.exp;
        createdWeapon.description = chosenRecipe.description;
        createdWeapon.image = chosenRecipe.image;
        createdWeapon.attack *= chosenRecipe.multiplier_physical_attack / chosenMaterials.Length;
        createdWeapon.attackSpeed *= chosenRecipe.multiplier_speed_attack / chosenMaterials.Length;
        createdWeapon.magicAttack *= chosenRecipe.multiplier_magic_attack / (chosenMaterials.Length * 1);
        createdWeapon.criticalHits *= chosenRecipe.multiplier_crytical_damage / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 3);
        createdWeapon.color /= chosenMaterials.Length;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdWeapon);

    }
    public void CreateNewArmor(ArmorRecipe chosenRecipe, Stone[] chosenMaterials)
    {
        Armor createdNewArmor = new Armor("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].flexibility;
            createdNewArmor.windDefence += chosenMaterials[i].resistance;
            createdNewArmor.frostDefence += chosenMaterials[i].resistance;
            createdNewArmor.frostDefence += chosenMaterials[i].density;
            createdNewArmor.magicDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.magicDefence += chosenMaterials[i].resistance;
            createdNewArmor.fireDefence += chosenMaterials[i].density;
            createdNewArmor.earthDefence += chosenMaterials[i].resistance;
            createdNewArmor.earthDefence += chosenMaterials[i].hardness;


            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / (chosenMaterials.Length * 2);
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / (chosenMaterials.Length * 2);
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / (chosenMaterials.Length * 2);
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewLegs(LegsRecipe chosenRecipe, Stone[] chosenMaterials)
    {
        Legs createdNewArmor = new Legs("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].flexibility;
            createdNewArmor.windDefence += chosenMaterials[i].resistance;
            createdNewArmor.frostDefence += chosenMaterials[i].resistance;
            createdNewArmor.frostDefence += chosenMaterials[i].density;
            createdNewArmor.magicDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.magicDefence += chosenMaterials[i].resistance;
            createdNewArmor.fireDefence += chosenMaterials[i].density;
            createdNewArmor.earthDefence += chosenMaterials[i].resistance;
            createdNewArmor.earthDefence += chosenMaterials[i].hardness;


            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / (chosenMaterials.Length * 2);
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / (chosenMaterials.Length * 2);
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / (chosenMaterials.Length * 2);
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewHelmet(HelmetRecipe chosenRecipe, Stone[] chosenMaterials)
    {
        Helmet createdNewArmor = new Helmet("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].flexibility;
            createdNewArmor.windDefence += chosenMaterials[i].resistance;
            createdNewArmor.frostDefence += chosenMaterials[i].resistance;
            createdNewArmor.frostDefence += chosenMaterials[i].density;
            createdNewArmor.magicDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.magicDefence += chosenMaterials[i].resistance;
            createdNewArmor.fireDefence += chosenMaterials[i].density;
            createdNewArmor.earthDefence += chosenMaterials[i].resistance;
            createdNewArmor.earthDefence += chosenMaterials[i].hardness;


            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / (chosenMaterials.Length * 2);
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / (chosenMaterials.Length * 2);
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / (chosenMaterials.Length * 2);
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewBoots(BootsRecipe chosenRecipe, Stone[] chosenMaterials)
    {
        Boots createdNewArmor = new Boots("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].flexibility;
            createdNewArmor.windDefence += chosenMaterials[i].resistance;
            createdNewArmor.frostDefence += chosenMaterials[i].resistance;
            createdNewArmor.frostDefence += chosenMaterials[i].density;
            createdNewArmor.magicDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.magicDefence += chosenMaterials[i].resistance;
            createdNewArmor.fireDefence += chosenMaterials[i].density;
            createdNewArmor.earthDefence += chosenMaterials[i].resistance;
            createdNewArmor.earthDefence += chosenMaterials[i].hardness;
            createdNewArmor.speed += chosenMaterials[i].hardness;

            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / (chosenMaterials.Length * 2);
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / (chosenMaterials.Length * 2);
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.speed *= chosenRecipe.multiplier_speed / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / (chosenMaterials.Length * 2);
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewGloves(GlovesRecipe chosenRecipe, Stone[] chosenMaterials)
    {
        Gloves createdNewArmor = new Gloves("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdNewArmor.windDefence += chosenMaterials[i].flexibility;
            createdNewArmor.windDefence += chosenMaterials[i].resistance;
            createdNewArmor.frostDefence += chosenMaterials[i].resistance;
            createdNewArmor.frostDefence += chosenMaterials[i].density;
            createdNewArmor.magicDefence += chosenMaterials[i].magicEfficiency;
            createdNewArmor.magicDefence += chosenMaterials[i].resistance;
            createdNewArmor.fireDefence += chosenMaterials[i].density;
            createdNewArmor.earthDefence += chosenMaterials[i].resistance;
            createdNewArmor.earthDefence += chosenMaterials[i].hardness;
            createdNewArmor.speedAtack += chosenMaterials[i].flexibility;

            createdNewArmor.defence += chosenMaterials[i].hardness;
            createdNewArmor.defence += chosenMaterials[i].density;
            createdNewArmor.color += chosenMaterials[i].color;

        }
        createdNewArmor.exp = chosenRecipe.exp;
        createdNewArmor.description = chosenRecipe.description;
        createdNewArmor.image = chosenRecipe.image;
        createdNewArmor.magicDefence *= chosenRecipe.multiplier_magicDefence / (chosenMaterials.Length * 2);
        createdNewArmor.frostDefence *= chosenRecipe.multiplier_frostDefence / (chosenMaterials.Length * 2);
        createdNewArmor.windDefence *= chosenRecipe.multiplier_windDefence / (chosenMaterials.Length * 2);
        createdNewArmor.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdNewArmor.speedAtack *= chosenRecipe.multiplier_speedAtack / chosenMaterials.Length;
        createdNewArmor.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdNewArmor.earthDefence *= chosenRecipe.multiplier_earthDefence / (chosenMaterials.Length * 2);
        createdNewArmor.color /= chosenMaterials.Length;
        createdNewArmor.lvl = chosenRecipe.lvl;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdNewArmor);

    }
    public void CreateNewNecklace(NecklaceRecipe chosenRecipe, Stone[] chosenMaterials)
    {
        Necklace createdWeapon = new Necklace("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdWeapon.attack += chosenMaterials[i].hardness;
            createdWeapon.attackSpeed += chosenMaterials[i].flexibility;
            createdWeapon.magicAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.criticalHits += chosenMaterials[i].strenght;
            createdWeapon.defence += chosenMaterials[i].resistance;

            createdWeapon.earthAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.earthAttack += chosenMaterials[i].hardness;
            createdWeapon.fireAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.waterAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.windAttack += chosenMaterials[i].magicEfficiency;

            createdWeapon.magicDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.fireDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.earthDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.frostDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.windDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.color += chosenMaterials[i].color;
        }
        createdWeapon.exp = chosenRecipe.exp;
        createdWeapon.description = chosenRecipe.description;
        createdWeapon.image = chosenRecipe.image;
        createdWeapon.attack *= chosenRecipe.multiplier_attack;
        createdWeapon.attackSpeed *= chosenRecipe.multiplier_attackSpeed / chosenMaterials.Length;
        createdWeapon.magicAttack *= chosenRecipe.multiplier_magicAttack / chosenMaterials.Length;
        createdWeapon.criticalHits *= chosenRecipe.multiplier_criticalHits / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / chosenMaterials.Length;

        createdWeapon.earthAttack *= chosenRecipe.multiplier_earthAttack / (chosenMaterials.Length * 2);
        createdWeapon.fireAttack *= chosenRecipe.multiplier_fireAttack / chosenMaterials.Length;
        createdWeapon.waterAttack *= chosenRecipe.multiplier_waterAttack / chosenMaterials.Length;
        createdWeapon.windAttack *= chosenRecipe.multiplier_windAttack / chosenMaterials.Length;

        createdWeapon.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdWeapon.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdWeapon.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdWeapon.frostDefence *= chosenRecipe.multiplier_frostDefence / chosenMaterials.Length;
        createdWeapon.windDefence *= chosenRecipe.multiplier_windDefence / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdWeapon.color /= chosenMaterials.Length;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdWeapon);
    }
    public void CreateNewRing(RingRecipe chosenRecipe, Stone[] chosenMaterials)
    {
        Ring createdWeapon = new Ring("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdWeapon.attack += chosenMaterials[i].hardness;
            createdWeapon.attackSpeed += chosenMaterials[i].flexibility;
            createdWeapon.magicAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.criticalHits += chosenMaterials[i].strenght;
            createdWeapon.defence += chosenMaterials[i].resistance;

            createdWeapon.earthAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.earthAttack += chosenMaterials[i].hardness;
            createdWeapon.fireAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.waterAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.windAttack += chosenMaterials[i].magicEfficiency;

            createdWeapon.magicDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.fireDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.earthDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.frostDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.windDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.color += chosenMaterials[i].color;
        }
        createdWeapon.exp = chosenRecipe.exp;
        createdWeapon.description = chosenRecipe.description;
        createdWeapon.image = chosenRecipe.image;
        createdWeapon.attack *= chosenRecipe.multiplier_attack;
        createdWeapon.attackSpeed *= chosenRecipe.multiplier_attackSpeed / chosenMaterials.Length;
        createdWeapon.magicAttack *= chosenRecipe.multiplier_magicAttack / chosenMaterials.Length;
        createdWeapon.criticalHits *= chosenRecipe.multiplier_criticalHits / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / chosenMaterials.Length;

        createdWeapon.earthAttack *= chosenRecipe.multiplier_earthAttack / (chosenMaterials.Length * 2);
        createdWeapon.fireAttack *= chosenRecipe.multiplier_fireAttack / chosenMaterials.Length;
        createdWeapon.waterAttack *= chosenRecipe.multiplier_waterAttack / chosenMaterials.Length;
        createdWeapon.windAttack *= chosenRecipe.multiplier_windAttack / chosenMaterials.Length;

        createdWeapon.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdWeapon.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdWeapon.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdWeapon.frostDefence *= chosenRecipe.multiplier_frostDefence / chosenMaterials.Length;
        createdWeapon.windDefence *= chosenRecipe.multiplier_windDefence / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdWeapon.color /= chosenMaterials.Length;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdWeapon);
    }

    //Wood
    public void CreateNewWeapon(WeaponRecipe chosenRecipe, Wood[] chosenMaterials)
    {
        Weapon createdWeapon = new Weapon("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdWeapon.attack += chosenMaterials[i].hardness;
            createdWeapon.attackSpeed += chosenMaterials[i].density;
            createdWeapon.magicAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.magicAttack += chosenMaterials[i].resistance;
            createdWeapon.criticalHits += chosenMaterials[i].flexibility;
            createdWeapon.defence += chosenMaterials[i].hardness;
            createdWeapon.defence += chosenMaterials[i].density;
            createdWeapon.color += chosenMaterials[i].color;
        }
        createdWeapon.exp = chosenRecipe.exp;
        createdWeapon.description = chosenRecipe.description;
        createdWeapon.image = chosenRecipe.image;
        createdWeapon.attack *= chosenRecipe.multiplier_physical_attack / chosenMaterials.Length;
        createdWeapon.attackSpeed *= chosenRecipe.multiplier_speed_attack / chosenMaterials.Length;
        createdWeapon.magicAttack *= chosenRecipe.multiplier_magic_attack / (chosenMaterials.Length * 2);
        createdWeapon.criticalHits *= chosenRecipe.multiplier_crytical_damage / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdWeapon.color /= chosenMaterials.Length;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdWeapon);
    }
    public void CreateNewTwoHandedWeapon(TwoHandedWeaponRecipe chosenRecipe, Wood[] chosenMaterials)
    {
        TwoHandedWeapon createdWeapon = new TwoHandedWeapon("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdWeapon.attack += chosenMaterials[i].hardness;
            createdWeapon.attackSpeed += chosenMaterials[i].density;
            createdWeapon.magicAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.magicAttack += chosenMaterials[i].resistance;
            createdWeapon.criticalHits += chosenMaterials[i].flexibility;
            createdWeapon.defence += chosenMaterials[i].hardness;
            createdWeapon.defence += chosenMaterials[i].density;
            createdWeapon.color += chosenMaterials[i].color;
        }
        createdWeapon.exp = chosenRecipe.exp;
        createdWeapon.description = chosenRecipe.description;
        createdWeapon.image = chosenRecipe.image;
        createdWeapon.attack *= chosenRecipe.multiplier_physical_attack / chosenMaterials.Length;
        createdWeapon.attackSpeed *= chosenRecipe.multiplier_speed_attack / chosenMaterials.Length;
        createdWeapon.magicAttack *= chosenRecipe.multiplier_magic_attack / (chosenMaterials.Length * 2);
        createdWeapon.criticalHits *= chosenRecipe.multiplier_crytical_damage / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdWeapon.color /= chosenMaterials.Length;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdWeapon);
    }
    public void CreateNewNecklace(NecklaceRecipe chosenRecipe, Wood[] chosenMaterials)
    {
        Necklace createdWeapon = new Necklace("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdWeapon.attack += chosenMaterials[i].hardness;
             createdWeapon.attackSpeed += chosenMaterials[i].flexibility;
            createdWeapon.magicAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.criticalHits += chosenMaterials[i].strenght;
            createdWeapon.defence += chosenMaterials[i].resistance;

            createdWeapon.earthAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.earthAttack += chosenMaterials[i].hardness;
            createdWeapon.fireAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.waterAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.windAttack += chosenMaterials[i].magicEfficiency;

             createdWeapon.magicDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.fireDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.earthDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.frostDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.windDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.color += chosenMaterials[i].color;
        }
        createdWeapon.exp = chosenRecipe.exp;
        createdWeapon.description = chosenRecipe.description;
        createdWeapon.image = chosenRecipe.image;
        createdWeapon.attack *= chosenRecipe.multiplier_attack;
        createdWeapon.attackSpeed *= chosenRecipe.multiplier_attackSpeed / chosenMaterials.Length;
        createdWeapon.magicAttack *= chosenRecipe.multiplier_magicAttack / chosenMaterials.Length;
        createdWeapon.criticalHits *= chosenRecipe.multiplier_criticalHits / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / chosenMaterials.Length;

        createdWeapon.earthAttack *= chosenRecipe.multiplier_earthAttack / (chosenMaterials.Length * 2);
        createdWeapon.fireAttack *= chosenRecipe.multiplier_fireAttack / chosenMaterials.Length;
        createdWeapon.waterAttack *= chosenRecipe.multiplier_waterAttack / chosenMaterials.Length;
        createdWeapon.windAttack *= chosenRecipe.multiplier_windAttack / chosenMaterials.Length;

        createdWeapon.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdWeapon.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdWeapon.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdWeapon.frostDefence *= chosenRecipe.multiplier_frostDefence / chosenMaterials.Length;
        createdWeapon.windDefence *= chosenRecipe.multiplier_windDefence / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdWeapon.color /= chosenMaterials.Length;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdWeapon);
    }
    public void CreateNewRing(RingRecipe chosenRecipe, Wood[] chosenMaterials)
    {
        Ring createdWeapon = new Ring("New " + chosenRecipe.name);

        for (int i = 0; i < chosenMaterials.Length; i++)
        {
            createdWeapon.attack += chosenMaterials[i].hardness;
            createdWeapon.attackSpeed += chosenMaterials[i].flexibility;
            createdWeapon.magicAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.criticalHits += chosenMaterials[i].strenght;
            createdWeapon.defence += chosenMaterials[i].resistance;

            createdWeapon.earthAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.earthAttack += chosenMaterials[i].hardness;
            createdWeapon.fireAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.waterAttack += chosenMaterials[i].magicEfficiency;
            createdWeapon.windAttack += chosenMaterials[i].magicEfficiency;

            createdWeapon.magicDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.fireDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.earthDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.frostDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.windDefence += chosenMaterials[i].magicEfficiency;
            createdWeapon.color += chosenMaterials[i].color;
        }
        createdWeapon.exp = chosenRecipe.exp;
        createdWeapon.description = chosenRecipe.description;
        createdWeapon.image = chosenRecipe.image;
        createdWeapon.attack *= chosenRecipe.multiplier_attack;
        createdWeapon.attackSpeed *= chosenRecipe.multiplier_attackSpeed / chosenMaterials.Length;
        createdWeapon.magicAttack *= chosenRecipe.multiplier_magicAttack / chosenMaterials.Length;
        createdWeapon.criticalHits *= chosenRecipe.multiplier_criticalHits / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / chosenMaterials.Length;

        createdWeapon.earthAttack *= chosenRecipe.multiplier_earthAttack / (chosenMaterials.Length * 2);
        createdWeapon.fireAttack *= chosenRecipe.multiplier_fireAttack / chosenMaterials.Length;
        createdWeapon.waterAttack *= chosenRecipe.multiplier_waterAttack / chosenMaterials.Length;
        createdWeapon.windAttack *= chosenRecipe.multiplier_windAttack / chosenMaterials.Length;

        createdWeapon.magicDefence *= chosenRecipe.multiplier_magicDefence / chosenMaterials.Length;
        createdWeapon.fireDefence *= chosenRecipe.multiplier_fireDefence / chosenMaterials.Length;
        createdWeapon.earthDefence *= chosenRecipe.multiplier_earthDefence / chosenMaterials.Length;
        createdWeapon.frostDefence *= chosenRecipe.multiplier_frostDefence / chosenMaterials.Length;
        createdWeapon.windDefence *= chosenRecipe.multiplier_windDefence / chosenMaterials.Length;
        createdWeapon.defence *= chosenRecipe.multiplier_defence / (chosenMaterials.Length * 2);
        createdWeapon.color /= chosenMaterials.Length;

        FindObjectOfType<ConversionTimeCreation>().AddItemToQueue(chosenMaterials, createdWeapon);
    }



    public bool checkRecipe(Recipe recipe, List<Object> items, System.Type type)
    {
        if (!recipeItemsIn(recipe, items))
        {
            return false;
        }

        int quantityWoods = 0;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].GetType().Equals(type))
            {
                quantityWoods += 1;
            }
        }
        if (!(recipe.materialCapacity == quantityWoods))
        {
            return false;
        }

        return true;
    }

    public bool itemIn(Object item, Object[] items)
    {
        for(int i =0; i < items.Length; i++)
        {
            if (items[i] != null && item.name == items[i].name)
            {
                return true;
            }
        }
        return false;
    }

    public bool recipeItemsIn(Recipe recipe, List<Object> items)
    {
        Object[] itemsCopy = new Object[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            itemsCopy[i] = items[i];
        }
        bool isIn = false;
        for (int i = 0; i < recipe.requiredItems.Length; i++)
        {
            isIn = false;
            for (int j = 0; j < items.Count; j++)
            {
                if (itemsCopy[j] != null && recipe.requiredItems[i].name == itemsCopy[j].name)
                {
                    itemsCopy[j] = null;
                    isIn = true;
                    break;
                }
            }

            if (isIn != true)
            {
                return false;

            }
        }
        return true;
    }


}
