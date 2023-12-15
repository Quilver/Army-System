using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Node<T>{
    //public Edge<T>[] paths;
    public Vector2 direction, position;
    public T data;
}
