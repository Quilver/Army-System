//From https://iquilezles.org/articles/intersectors/
// For intersections of lines with circles, rectangles, triangles, and other lines
// Returns: d values of the intersection points, or (-1,-1) if no intersection
// Assumptions: Direction is normalised, Circle/Rectangle are centered at the origin, Ray is relative to Circle/Rectangle
#ifndef GeometryLib
#define GeometryLib

float2 sphIntersect(float3 rayStart, float3 direction, float ra)
{
    float b = dot(rayStart, direction);
    float c = dot(rayStart, rayStart) - ra * ra;
    float h = b * b - c;
    if (h < 0.0)
        return float2(-1,-1); // no intersection
    h = sqrt(h);
    return float2(-b - h, -b + h);
}
bool pointInCircle(float3 position, float radius)
{
    return dot(position, position) < radius * radius;
}
// axis aligned, center at the origin, dimensions "boxSize"
float2 boxIntersection(float3 rayStart, float3 direction, float3 boxSize)
{
    float2 m = 1.0 / direction.xy; // can precompute if traversing a set of aligned boxes
    float2 n = m * rayStart.xy; // can precompute if traversing a set of aligned boxes, time to intersect with axis
    float2 k = abs(m) * boxSize.xy;
    float2 t1 = -n - k;
    float2 t2 = -n + k;
    float tN = max(t1.x, t1.y);
    float tF = min(t2.x, t2.y);
    if (tN > tF || tF < 0.0)//tF < 0.0 || tN > 0.0)
        return float2(-1,-1); // no intersection
    return float2(tN, tF);
}
bool pointInSquare(float3 position, float3 boxSize)
{
    return abs(position.x) < boxSize.x && abs(position.y) < boxSize.y;
}
//I wrote this one, not from iq's article, so it may be wrong
float lineIntersect(float3 rayStart, float3 rayVector, float3 lineStart, float3 lineEnd)
{
    float3 lineTransposed = float3(lineStart.y - lineEnd.y, lineEnd.x - lineEnd.x, 0);
    float angle = dot(rayVector, lineTransposed);
    if (angle == 0)
    {
        return -1;
    }
    return -dot(lineTransposed, (rayStart - lineStart)) / angle;
}

//v0,v1,v2 are the vertices of the triangle
float3 triIntersect(float3 rayStart, float3 direction, float3 v0, float3 v1, float3 v2)
{
    float3 v1v0 = v1 - v0;
    float3 v2v0 = v2 - v0;
    float3 rov0 = rayStart - v0;
    float3 n = cross(v1v0, v2v0);
    float3 q = cross(rov0, direction);
    float d = 1.0 / dot(direction, n);
    float u = d * dot(-q, v2v0);
    float v = d * dot(q, v1v0);
    float t = d * dot(-n, rov0);
    if (u < 0.0 || v < 0.0 || (u + v) > 1.0)
        t = -1.0;
    return float3(t, u, v);
}


//personally written, for my code in specific
float3 getPixelOffset(uint3 pixel, float resolution, float worldUnits)
{
    float2 offset = worldUnits * (pixel.xy / resolution - 0.5);
    return float3(offset.xy, 0);
}
float2 NormaliseIntersections(float2 intersections, float magnitude)
{
    
    if (intersections.x > magnitude || intersections.y < 0)
        return float2(-1, -1);
    intersections = intersections / magnitude;
    intersections.x = max(intersections.x, 0);
    intersections.y = min(intersections.y, 1);
    return intersections;
}
float2 getBoundingCircleIntersection(float3 rayStart, float3 direction, float magnitude, float radius)
{
    if (magnitude < 0.001)
    {
        if (pointInCircle(rayStart, radius))
            return float2(0, 1);
        else
            return float2(-1, -1);
    }
    float2 intersections = sphIntersect(rayStart, direction, radius);
    return NormaliseIntersections(intersections, magnitude);
}
float2 getBoundingBoxIntersection(float3 rayStart, float3 direction, float magnitude, float3 boxSize)
{
    if (magnitude < 0.001 || (direction.x == 0 && direction.y == 0))
    {
        if (pointInSquare(rayStart, boxSize))
            return float2(-1, -1);
        else
            return float2(-1, -1);
    }
    else if (direction.x == 0)
    {
        if(abs(rayStart.x) > boxSize.x)return float2(-1, -1);
    }
    else if(direction.y == 0)
    {
        if (abs(rayStart.y) > boxSize.y)return float2(-1, -1);
    }
    float2 intersections = boxIntersection(rayStart, direction, boxSize);
    return NormaliseIntersections(intersections, magnitude);
}
struct Circle
{
    float3 position, direction;
    float speed, radius;
};
struct Box
{
    float3 position, direction;
    float2 size;
    float speed, rotation, angularVelocity, boundingRadius;
    int intersectionTests;
};

#endif