using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelAnimationTest : MonoBehaviour
{
    [SerializeField]
    int x, y;
    [SerializeField]
    bool attack;
    [SerializeField]
    bool moving;
    Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        animator= GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (attack)
        {
            attack= false;
            animator.Play("Attack");
        }
        animator.SetFloat("Horizontal", x);
        //animator.SetFloat("Horiz", x);
        animator.SetFloat("Veritcle", y);
        animator.SetBool("Moving", moving);
    }
}
