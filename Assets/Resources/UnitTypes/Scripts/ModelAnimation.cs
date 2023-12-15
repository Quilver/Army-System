using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelAnimation : MonoBehaviour
{
    private static readonly int Vertical = Animator.StringToHash("Vertical");
    private static readonly int Horizontal = Animator.StringToHash("Horizontal");
    private static readonly float LoopDuration = 2f;

    private Coroutine _loopDelayRoutine;
    private Vector2 _playerRotation = Vector2.zero;
    private Animator _animator;
    private AnimationClip[] _animationClips;
    private int _clipIndex;
    private string _enabledParam = String.Empty;
    private void Start()
    {
        _animator = GetComponent<Animator>();
    }
    private void OnEnable()
    {
        ResetRotation();
    }
    public void Idle()
    {
        _animator.SetBool("Walk", false);
    }
    public void Walk()
    {
        _animator.SetBool("Walk", true);
        _animator.Play("Walk");
    }
    public void Attack()
    {
        _animator.Play("Attack");
    }
    public void Die()
    {

    }
    public void SetRotation(int x, int y)
    {
        _playerRotation.x=x; _playerRotation.y=y;
        UpdateParams();
    }
    private void ResetRotation()
    {
        _playerRotation.x = 0;
        _playerRotation.y = 0;
    }

    private void UpdateParams()
    {
        _animator.SetFloat("Horizontal", _playerRotation.x);
        _animator.SetFloat("Vertical", _playerRotation.y);
    }
}
