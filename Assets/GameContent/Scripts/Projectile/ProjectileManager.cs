using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileManager : MonoBehaviour, IResettable
{
    public static ProjectileManager Instance;
    private Transform spawnParent;

    [HideInInspector] public List<Projectile> allProjectiles = new List<Projectile>();


    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        spawnParent = GameObject.Find("ProjectileParent").transform;
    }

    public void UpdateNormal()
    {
        for (int i = allProjectiles.Count - 1; i >= 0; i--)
        {
            var p = allProjectiles[i];
            if (!p) { allProjectiles.RemoveAt(i); continue; }
            p.UpdateNormal();
        }
    }


    public Projectile spawnProjectile(GameObject projectilePrefab, Vector2 position)
    {
        GameObject projectileObject = Instantiate(projectilePrefab, position, Quaternion.identity, spawnParent);
        Projectile projectile = projectileObject.GetComponent<Projectile>();
        allProjectiles.Add(projectile);
        return projectile;
    }

    public void RemoveProjectile(Projectile projectile, float delay = 0f)
    {
        allProjectiles.Remove(projectile);
        Destroy(projectile.gameObject, delay);
    }

    public void RemoveAllProjectiles()
    {
        foreach (Projectile p in allProjectiles) 
            { Destroy(p.gameObject); }
        allProjectiles.Clear();
    }

    public void StoreInit()
    {

    }

    public void ResetScript()
    {
        // runtime clear
        for (int i = allProjectiles.Count - 1; i >= 0; i--)
        {
            if (allProjectiles[i])
                Destroy(allProjectiles[i].gameObject);
        }
        allProjectiles.Clear();
    }

}
