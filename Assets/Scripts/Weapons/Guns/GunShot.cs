using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunShot : MonoBehaviour
{
    public GameObject impact;

    public float t { get; set; }

    private Vector3 moveDir;
    private float spd;
    private float gvy;

    private Vector3 gvyAcc = new Vector3(0, 0, 0);
    private Vector3 lastPos;

    private RaycastHit hit;

    public void Setup(Vector3 startPos, Vector3 target, float speed, float gravity = 0) {
        lastPos = transform.position = startPos;
        transform.LookAt(target);

        moveDir = (target - startPos).normalized;
        spd = speed;
        gvy = gravity;

        t = 3;
    }

    public void OnUpdate(Gun g) {
        t -= Time.deltaTime;
        if (t <= 0) {
            g.shots.Remove(this);
            Destroy(gameObject);
            return;
        }

        gvyAcc -= Vector3.down * 9.81f * Time.deltaTime * gvy;
        transform.position += moveDir * spd * Time.deltaTime * 60 + (gvyAcc * Time.deltaTime);

        if (Physics.Raycast(lastPos, transform.position - lastPos, out hit, (transform.position - lastPos).magnitude)) {
            Instantiate(impact, hit.point, Quaternion.LookRotation(hit.normal));
            g.shots.Remove(this);
            Destroy(gameObject);
            return;
        }
        lastPos = transform.position;
    }
}
