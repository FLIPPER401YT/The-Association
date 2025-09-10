using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

class PlayerShoot : MonoBehaviour
{
    [SerializeField] int damage;
    [SerializeField] float fireRate;
    [SerializeField] float fireDistance;
    [SerializeField] int bullets;
    [SerializeField] float bulletSpeed;
    [SerializeField] int ammo;
    [SerializeField] bool isAutomatic;
    [SerializeField] GameObject shootPoint;
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] LayerMask shootMask;

    float fireTimer = 0;

    public void Shoot()
    {
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireRate && ammo > 0 && isAutomatic ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1"))
        {
            for (int bullet = 0; bullet < bullets; bullet++)
            {
                RaycastHit hit;
                if (Physics.Raycast(shootPoint.transform.position, shootPoint.transform.forward, out hit, fireDistance, shootMask))
                {
                    IDamage dmg = hit.transform.GetComponent<IDamage>();
                    if (dmg != null)
                    {
                        dmg.TakeDamage(damage);
                    }
                }
                Bullet bulletObj = Instantiate(bulletPrefab, shootPoint.transform.position, Camera.main.transform.rotation, null).GetComponent<Bullet>();
                bulletObj.targetPoint = hit.point;
                bulletObj.distance = fireDistance;
                bulletObj.bulletSpeed = bulletSpeed;// * (hit.transform != null ? Vector3.Distance(shootPoint.transform.position, hit.point) : );
            }
            ammo--;
            fireTimer = 0;
        }
    }
}