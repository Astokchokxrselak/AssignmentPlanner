using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Common;

public class _Ropify : MobileEntity
{
    private Vector2 size;
    public int jointCount;
    // Start is called before the first frame update

    Rigidbody2D lastBlock, firstBlock;
    void Start()
    {
        size = BoundsSize;
        float deltaX = size.x / jointCount;
        GameObject rope = new GameObject("Rope");
        rope.transform.position = transform.position;
        
        for (int i = -jointCount / 2, j = 0; i < jointCount / 2; i++, j++) {
            float nextX = deltaX / 2 + deltaX * i;

            var block = Instantiate(gameObject, Vector3.zero, Quaternion.identity, rope.transform).transform;
            block.localScale = new(block.localScale.x / jointCount, block.localScale.y);
            block.localPosition = new(nextX, 0);
            Destroy(block.GetComponent<_Ropify>());

            if (lastBlock) {
                Bind(block.gameObject, lastBlock);
            }
            lastBlock = block.GetComponent<Rigidbody2D>();
            if (!firstBlock) {
                firstBlock = lastBlock;
            }

            if (j == jointCount - 1) {
                var onCollide = lastBlock.gameObject.AddComponent<OnCollideEvent>();
                onCollide.OnCollide += (other, col) => {
                    if (other.attachedRigidbody == firstBlock) {
                        Bind(block.gameObject, firstBlock);
                    }
                };
            }
        }
        Destroy(gameObject);
    }
    public float maxForce;
    void Bind(GameObject o, Rigidbody2D t) {
        var joint = o.AddComponent<HingeJoint2D>();
        joint.connectedBody = t;
        joint.autoConfigureConnectedAnchor = true;
        joint.breakForce = maxForce;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
