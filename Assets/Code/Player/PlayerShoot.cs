using System.Collections.Generic;
using UnityEngine;

class PlayerShoot : MonoBehaviour
{
    [SerializeField] int damage;
    [SerializeField] float fireRate;
    [SerializeField] float fireDistance;
    [SerializeField] int bullets;
    [SerializeField] float bulletSpeed;
    [SerializeField] float bloomMod;
    [SerializeField] int ammo;
    [SerializeField] bool isAutomatic;
    [SerializeField] GameObject shootPoint;
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] LayerMask shootMask;
    [SerializeField] List<GunStats> gunList;
    [SerializeField] MeleeStats meleeStats;

    float fireTimer = 0;
    bool isMelee = false;

    void Start()
    {
        foreach (GunStats stat in gunList) ReloadWeapon(stat);
        SwitchWeapons(gunList[0]);
    }

    void Update()
    {
        if (Input.GetButtonDown("Weapon1"))
        {
            SwitchWeapons(gunList[0]);
            isMelee = false;
        }
        else if (gunList.Count > 1 && Input.GetButtonDown("Weapon2"))
        {
            SwitchWeapons(gunList[1]);
            isMelee = false;
        }
        else if (Input.GetButtonDown("Weapon3"))
        {
            SwitchWeapons(meleeStats);
            isMelee = true;
        }
    }

    void ReloadWeapon(GunStats stat)
    {
        stat.ammo = stat.maxAmmo;
    }

    public void Shoot()
    {
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireRate && ammo > 0 && (isAutomatic ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1")))
        {
            for (int bullet = 0; bullet < bullets; bullet++)
            {
                RaycastHit hit;
                float rangeX = Random.Range(-bloomMod, bloomMod);
                float rangeY = Random.Range(-bloomMod, bloomMod);
                if (Physics.Raycast(shootPoint.transform.position,
                                    new Vector3(Camera.main.transform.forward.x + rangeX,
                                                Camera.main.transform.forward.y + rangeY,
                                                Camera.main.transform.forward.z),
                                    out hit,
                                    fireDistance,
                                    shootMask))
                {
                    IDamage dmg = hit.transform.GetComponent<IDamage>();
                    if (dmg != null)
                    {
                        dmg.TakeDamage(damage);
                    }
                }
                if (!isMelee)
                {
                    Bullet bulletObj = Instantiate(bulletPrefab, shootPoint.transform.position, new Quaternion(Camera.main.transform.rotation.x + rangeX, Camera.main.transform.rotation.y + rangeY, Camera.main.transform.rotation.z, Camera.main.transform.rotation.w), null).GetComponent<Bullet>();
                    bulletObj.targetPoint = hit.point;
                    bulletObj.distance = fireDistance;
                    bulletObj.bulletSpeed = bulletSpeed;
                }
            }
            if (!isMelee) ammo--;
            fireTimer = 0;
        }
    }

    void SwitchWeapons(GunStats stats)
    {
        damage = stats.damage;
        fireRate = stats.fireRate;
        fireDistance = stats.fireDistance;
        bloomMod = stats.bloomMod;
        bullets = stats.bullets;
        ammo = stats.ammo;
        isAutomatic = stats.isAutomatic;
    }

    void SwitchWeapons(MeleeStats stats)
    {
        damage = stats.damage;
        fireRate = stats.swingRate;
        fireDistance = stats.swingDistance;
        bullets = 1;
        ammo = 1;
        bloomMod = 0;
    }
}