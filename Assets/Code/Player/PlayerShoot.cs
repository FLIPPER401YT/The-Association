using System.Collections.Generic;
using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [SerializeField] int damage;
    [SerializeField] float fireRate;
    [SerializeField] float fireDistance;
    [SerializeField] int bullets;
    [SerializeField] float bulletSpeed;
    [SerializeField] float bloomMod;
    [SerializeField] bool isAutomatic;
    [SerializeField] GameObject shootPoint;
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] LayerMask shootMask;
    [SerializeField] List<GunStats> gunList;
    [SerializeField] MeleeStats meleeStats;
    [SerializeField] MeshFilter weaponMesh;
    [SerializeField] Renderer weaponRenderer;
    [SerializeField] Animator weaponAnimator;

    float fireTimer = 0;
    bool isMelee = false;
    int gunListPos = 0;

    void Start()
    {
        foreach (GunStats stat in gunList) FillAmmo(stat);
        SwitchWeapons(gunList[0], 0);
    }

    void Update()
    {
        if (Input.GetButtonDown("Weapon1"))
        {
            SwitchWeapons(gunList[0], 0);
            isMelee = false;
        }
        else if (gunList.Count > 1 && Input.GetButtonDown("Weapon2"))
        {
            SwitchWeapons(gunList[1], 1);
            isMelee = false;
        }
        else if (Input.GetButtonDown("Weapon3"))
        {
            SwitchWeapons(meleeStats);
            isMelee = true;
        }
    }

    void FillAmmo(GunStats stat)
    {
        stat.ammo = stat.maxAmmo;
        stat.clip = stat.clipSize;
    }

    void ReloadGun(GunStats stat)
    {
        int reloadAmount = stat.clipSize - stat.clip;
        if (stat.ammo >= reloadAmount)
        {
            stat.ammo -= reloadAmount;
            stat.clip = stat.clipSize;
        }
        else
        {
            stat.clip += stat.ammo;
            stat.ammo = 0;
        }
        
    }

    public void AddAmmo(int amount)
    {
        if (!isMelee)
        {
            if (gunList[gunListPos].clip < gunList[gunListPos].clipSize)
            {
                amount -= gunList[gunListPos].clipSize - gunList[gunListPos].clip;
                gunList[gunListPos].clip = gunList[gunListPos].clipSize;
            }
            gunList[gunListPos].ammo += amount;
            gunList[gunListPos].ammo = Mathf.Clamp(gunList[gunListPos].ammo, 0, gunList[gunListPos].maxAmmo);
        }
    }

    public void Reload()
    {
        if (!isMelee && Input.GetButtonDown("Reload"))
        {
            ReloadGun(gunList[gunListPos]);
        }
    }

    public void Shoot()
    {
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireRate && (isMelee || gunList[gunListPos].clip > 0) && (isAutomatic ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1")))
        {
            //float totalDamage = 0;
            List<IDamage> damages = new List<IDamage>();
            for (int bullet = 0; bullet < bullets; bullet++)
            {
                RaycastHit hit;
                Vector3 randomSpread = Random.insideUnitSphere * (bloomMod / 100.0f);
                if (Physics.Raycast(shootPoint.transform.position,
                                    (shootPoint.transform.forward + randomSpread).normalized,
                                    out hit,
                                    fireDistance,
                                    shootMask))
                {
                    IDamage dmg = hit.transform.GetComponent<IDamage>();
                    if (dmg != null)
                    {
                        //totalDamage += damage;
                        damages.Add(dmg);
                    }
                }
                if (!isMelee)
                {
                    Bullet bulletObj = Instantiate(bulletPrefab, shootPoint.transform.position, Quaternion.LookRotation((shootPoint.transform.forward + randomSpread).normalized), null).GetComponent<Bullet>();
                    bulletObj.targetPoint = hit.point;
                    bulletObj.distance = fireDistance;
                    bulletObj.bulletSpeed = bulletSpeed;
                }
            }

            foreach (IDamage dmg in damages) if (dmg != null) dmg.TakeDamage(damage);
            
            if (!isMelee)
            {
                gunList[gunListPos].clip--;
                weaponAnimator.SetTrigger("Ranged");
            }
            else weaponAnimator.SetTrigger("Melee");
            fireTimer = 0;
        }
    }

    void SwitchWeapons(GunStats stats, int pos)
    {
        damage = stats.damage;
        fireRate = stats.fireRate;
        fireDistance = stats.fireDistance;
        bloomMod = stats.bloomMod;
        bullets = stats.bullets;
        isAutomatic = stats.isAutomatic;
        weaponMesh.sharedMesh = stats.model.GetComponent<MeshFilter>().sharedMesh;
        weaponRenderer.sharedMaterial = stats.model.GetComponent<Renderer>().sharedMaterial;
        gunListPos = pos;
    }

    void SwitchWeapons(MeleeStats stats)
    {
        damage = stats.damage;
        fireRate = stats.swingRate;
        fireDistance = stats.swingDistance;
        bullets = 1;
        bloomMod = 0;
        weaponMesh.sharedMesh = stats.model.GetComponent<MeshFilter>().sharedMesh;
        weaponRenderer.sharedMaterial = stats.model.GetComponent<Renderer>().sharedMaterial;
    }
}