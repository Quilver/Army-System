using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Formation
{
    public interface IShape 
    {
        public int Ranks { get; }
        Vector2 OffsetFromUnit { get; }
        Vector2 SizeOfFormation {  get; }
        
    }
}