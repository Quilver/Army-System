using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Edge<T>{
    public Node<T> node;
    public int weight;
    public Vector2 direction;
}
public class minHeap<T>
{
    public class N
    {
        public int score;
        public T data;
    }
    public int size;
    public N[] mH;
    //bool working = true;
    public minHeap(int size)
    {
        this.size = 0;
        mH = new N[size + 1];
    }
    //adds a node to the heap
    public void insert(T info, int x)
    {
        N n = new N
        {
            data = info,
            score = x
        };
        //Debug.Log("inserting item to heap");
        mH[size] = n;
        bubbleUp(size);
        size++;
        /*if (working && !valid())
        {
            working = false;
            Debug.Log("The insert doesn't work");
        }*/
    }
    //
    public void bubbleUp(int index)
    {
       while ((index > 0) && (mH[Parent(index)].score > mH[index].score))
        {
            int original_parent_pos = Parent(index);
            swap(index, original_parent_pos);
            index = original_parent_pos;
        }
    }
    //gets node with the lowest value
    public T extractMin()
    {
        N min = mH[0];
        //Debug.Log("getting first item from heap");
        mH[0] = mH[size - 1];
        mH[size - 1] = null;
        size--;
        sinkDown(0);
        /*if (working && !valid())
        {
            working = false;
            Debug.Log("The extractMin doesn't work");
        }*/
        return min.data;
    }
    //gets parent index node of the current node
    static int Parent(int position)
    {
        return (position - 1) / 2;
    }
    //
    public void sinkDown(int k)
    {
        int test;
        if(2 * k + 2 < size) {
            if(mH[2 * k +  1].score < mH[2 * k + 2].score) { test = 2 * k + 1; }
            else { test = 2 * k + 2; }
        }
        else if (2 * k + 1 < size) {
            test = 2 * k + 1;
        }
        else
        {
            return;
        }
        if (mH[k].score > mH[test].score)
        {
            swap(k, test);
           sinkDown(test);
        }

    }
    //swaps the positition of 2 nodes in the array
    public void swap(int a, int b)
    {
        N temp = mH[a];
        mH[a] = mH[b];
        mH[b] = temp;
    }
    public void displayScores()
    {
        string nodes = "";
        
        for (int i = 0; i < size && i < 20; i++)
        {
            nodes += mH[i].score;
            nodes += " ";
        }
        Debug.Log(nodes);
    }
    bool valid()
    {
        ///*
        for (int i = 0; i < size; i++)
        {
            if (mH[0].score > mH[i].score)
            {
                return false;
            }
        }//*/
        return true;
    }
}