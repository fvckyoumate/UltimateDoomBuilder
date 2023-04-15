#pragma once

static const char* TraceGLSL = R"(

struct CollisionNode
{
	vec3 center;
	float padding1;
	vec3 extents;
	float padding2;
	int left;
	int right;
	int element_index;
	int padding3;
};

layout(std430, set = 0, binding = 0) buffer NodeBuffer
{
	int nodesRoot;
	int nodebufferPadding1;
	int nodebufferPadding2;
	int nodebufferPadding3;
	CollisionNode nodes[];
};

layout(std430, set = 0, binding = 1) buffer VertexBuffer { vec4 vertices[]; };
layout(std430, set = 0, binding = 2) buffer ElementBuffer { int elements[]; };

struct RayBBox
{
	vec3 start, end;
	vec3 c, w, v;
};

RayBBox create_ray(vec3 ray_start, vec3 ray_end)
{
	RayBBox ray;
	ray.start = ray_start;
	ray.end = ray_end;
	ray.c = (ray_start + ray_end) * 0.5;
	ray.w = ray_end - ray.c;
	ray.v = abs(ray.w);
	return ray;
}

bool overlap_bv_ray(RayBBox ray, int a)
{
	vec3 v = ray.v;
	vec3 w = ray.w;
	vec3 h = nodes[a].extents;
	vec3 c = ray.c - nodes[a].center;

	if (abs(c.x) > v.x + h.x ||
		abs(c.y) > v.y + h.y ||
		abs(c.z) > v.z + h.z)
	{
		return false;
	}

	if (abs(c.y * w.z - c.z * w.y) > h.y * v.z + h.z * v.y ||
		abs(c.x * w.z - c.z * w.x) > h.x * v.z + h.z * v.x ||
		abs(c.x * w.y - c.y * w.x) > h.x * v.y + h.y * v.x)
	{
		return false;
	}

	return true;
}

#define FLT_EPSILON 1.192092896e-07F // smallest such that 1.0+FLT_EPSILON != 1.0

float intersect_triangle_ray(RayBBox ray, int a, out float barycentricB, out float barycentricC)
{
	int start_element = nodes[a].element_index;

	vec3 p[3];
	p[0] = vertices[elements[start_element]].xyz;
	p[1] = vertices[elements[start_element + 1]].xyz;
	p[2] = vertices[elements[start_element + 2]].xyz;

	// Moeller-Trumbore ray-triangle intersection algorithm:

	vec3 D = ray.end - ray.start;

	// Find vectors for two edges sharing p[0]
	vec3 e1 = p[1] - p[0];
	vec3 e2 = p[2] - p[0];

	// Begin calculating determinant - also used to calculate u parameter
	vec3 P = cross(D, e2);
	float det = dot(e1, P);

	// Backface check
	//if (det < 0.0f)
	//	return 1.0f;

	// If determinant is near zero, ray lies in plane of triangle
	if (det > -FLT_EPSILON && det < FLT_EPSILON)
		return 1.0f;

	float inv_det = 1.0f / det;

	// Calculate distance from p[0] to ray origin
	vec3 T = ray.start - p[0];

	// Calculate u parameter and test bound
	float u = dot(T, P) * inv_det;

	// Check if the intersection lies outside of the triangle
	if (u < 0.f || u > 1.f)
		return 1.0f;

	// Prepare to test v parameter
	vec3 Q = cross(T, e1);

	// Calculate V parameter and test bound
	float v = dot(D, Q) * inv_det;

	// The intersection lies outside of the triangle
	if (v < 0.f || u + v  > 1.f)
		return 1.0f;

	float t = dot(e2, Q) * inv_det;
	if (t <= FLT_EPSILON)
		return 1.0f;

	// Return hit location on triangle in barycentric coordinates
	barycentricB = u;
	barycentricC = v;
	
	return t;
}

bool is_leaf(int node_index)
{
	return nodes[node_index].element_index != -1;
}

bool TraceAnyHit(vec3 origin, float tmin, vec3 dir, float tmax)
{
	if (tmax <= 0.0f)
		return false;

	RayBBox ray = create_ray(origin, origin + dir * tmax);
	tmin /= tmax;

	int stack[64];
	int stackIndex = 0;
	stack[stackIndex++] = nodesRoot;
	do
	{
		int a = stack[--stackIndex];
		if (overlap_bv_ray(ray, a))
		{
			if (is_leaf(a))
			{
				float baryB, baryC;
				float t = intersect_triangle_ray(ray, a, baryB, baryC);
				if (t >= tmin && t < 1.0)
				{
					return true;
				}
			}
			else
			{
				stack[stackIndex++] = nodes[a].right;
				stack[stackIndex++] = nodes[a].left;
			}
		}
	} while (stackIndex > 0);
	return false;
}

)";
