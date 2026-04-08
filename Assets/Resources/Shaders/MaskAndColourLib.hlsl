#ifndef MaskAndColourLib
#define MaskAndColourLib
#define MASKRANGE 32
#define STEPSIZE 1.0/32.0
uint BoolOverlapMask(float2 intersections, uint mask)
{
    if (intersections.y == -1)
        return mask;
   
    [unroll(MASKRANGE)]
    for (int i = 0; i < MASKRANGE; i++)
    {
        float sliceStart = i * STEPSIZE;
        float sliceEnd = (i + 1) * STEPSIZE;

        // Check for overlap: 
        // A slice overlaps if its start is before the range end 
        // AND its end is after the range start.
        if (sliceStart < intersections.y && sliceEnd > intersections.x)
            mask |= (1 << i);
    }
    return mask;

}

float4 GetPixelColour(float start, float end)
{
    float overlap = end - start;
    float time = (start + end) / 2;
    return float4((1 - time) / overlap, 0, time / overlap, overlap);
}


float4 GetPixelColour(uint mask)
{
    if (mask == 0)
        return float4(0, 0, 0, 0);
    float total = 0;
    float weightedAverage = 0;
    float early = -1;
    [unroll(MASKRANGE)]
    for (int i = 0; i < MASKRANGE; i++)
    {
        // Simple "is bit set?" check
        if (mask & (1 << i))
        {
            total++;
            weightedAverage += i + 1;
            if (early == -1)
                early = i * STEPSIZE;
        }
    }
    weightedAverage *= STEPSIZE/total;
    total *= STEPSIZE;
    return float4(1 - weightedAverage, 0, weightedAverage, total);
}
float4 GetPixelColour(float2 intersections)
{
    if (intersections.y < 0)
        return float4(0, 0, 0, 0);
    return GetPixelColour(BoolOverlapMask(intersections, 0));

}


uint GetGridIndex(float2 position, uint2 gridDimension, float2 unitsPerCell)
{ //Need to handle units per cell to allow for different grid sizes, but for now we can just assume 1 unit per cell
    float2 gridSpace = (position + (gridDimension / 2.0f));
    uint2 gridIndex = gridSpace;
    if (gridIndex.x < 0 || gridIndex.y < 0 || gridIndex.x >= gridDimension.x || gridIndex.y >= gridDimension.y)
        return -1;
    return gridIndex.x + gridIndex.y * gridDimension.x;
}
#endif