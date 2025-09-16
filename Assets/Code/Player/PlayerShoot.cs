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
    [SerializeField] GameObject hitEffect;
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
        if(gunList.Count > 0)
        {
            if (isMelee == false)
            {
                GameManager.instance.ammoUIObject.SetActive(true);
                GameManager.instance.currentAmmo.text = gunList[gunListPos].clip.ToString("F0");
                GameManager.instance.totalAmmo.text = gunList[gunListPos].ammo.ToString("F0");
            }
            else
            {
                GameManager.instance.ammoUIObject.SetActive(false);
            }
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

    public void AddAmmoAll(int amount)
    {
        foreach (GunStats gun in gunList)
        {
            if (gun.clip < gun.clipSize)
            {
                amount -= gun.clipSize - gun.clip;
                gun.clip = gun.clipSize;
            }
            gun.ammo += amount;
            gun.ammo = Mathf.Clamp(gun.ammo, 0, gun.maxAmmo);
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
        bool heavyAttack = false;
        if (fireTimer >= fireRate)
        {
            if (isMelee && Input.GetButtonDown("Fire2"))
            {
                heavyAttack = true;
                fireRate *= 1.5f;
            }
            else fireRate = meleeStats.swingRate;

            if ((isMelee || gunList[gunListPos].clip > 0) && ((isAutomatic ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1")) || (isMelee && Input.GetButtonDown("Fire2"))))
            {
                Dictionary<IDamage, int> damages = new Dictionary<IDamage, int>();
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
                            if (damages.ContainsKey(dmg)) damages[dmg] += isMelee && heavyAttack ? damage * 2 : damage;
                            else damages.Add(dmg, isMelee && heavyAttack ? damage * 2 : damage);
                            Instantiate(hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
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

                foreach (KeyValuePair<IDamage, int> dmg in damages) dmg.Key.TakeDamage(dmg.Value);

                if (!isMelee)
                {
                    gunList[gunListPos].clip--;
                    weaponAnimator.SetTrigger("Ranged");
                }
                else weaponAnimator.SetTrigger("Melee");
                fireTimer = 0;
            }
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