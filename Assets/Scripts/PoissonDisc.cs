using System.Collections.Generic;
using UnityEngine;

public static class PoissonDisc
{
    private static readonly int MAX_ATTEMPTS = 30;
    private const float TAU = 6.28318530718f;

    /// <summary>
    /// Generates an even Poisson Distribution of points on a disc, with a max radius and minimum spread of points
    /// https://en.wikipedia.org/wiki/Poisson_distribution
    /// </summary>
    public static List<Vector2> GeneratePoints(float spread, float maxRadius)
    {
        List<Vector2> points = new List<Vector2>();

        List<Vector2> newPoints = new List<Vector2>();

        Vector2 initialPoint = Random.insideUnitCircle;

        //Warm sampler with initial point
        newPoints.Add(initialPoint);

        //Whilst we potentially still have points
        while (newPoints.Count > 0)
        {
            //Get a random point to check
            int index = Random.Range(0, newPoints.Count);
            Vector2 centre = newPoints[index];
            bool success = false;

            //Try each point up to our limit
            for (int i = 0; i < MAX_ATTEMPTS; i++)
            {
                //Generate a point from a random direction
                float radAngle = Random.value * TAU;

                Vector2 direction = new Vector2(Mathf.Sin(radAngle), Mathf.Cos(radAngle));
                direction = centre + direction * Random.Range(spread, spread * 2.0f);

                //Check position to see if it's alright
                if (!ValidPosition(direction, spread, maxRadius, points)) continue;
                //Add to final points and add to points to check from
                newPoints.Add(direction);

                points.Add(direction);

                success = true;
            }

            //Remove this point from the checklist as it's probably not got any room near it
            if (!success)
            {
                newPoints.RemoveAt(index);
            }
        }

        return points;
    }

    /// <summary>
    /// Checks the validity of a position, assuming that the centre of the disc is world zero.
    /// </summary>
    private static bool ValidPosition(Vector3 point, float spread, float maxRadius, IEnumerable<Vector2> points)
    {
        float sqSpread = spread * spread;
        float sqRadius = maxRadius * maxRadius;

        //If it's off the disc, return early
        if (point.sqrMagnitude > sqRadius)
        {
            return false;
        }

        //Otherwise, check every point to see if it's far enough away
        foreach (Vector3 p in points)
        {
            float sqDistance = (point - p).sqrMagnitude;

            //Point is too close
            if (sqDistance < sqSpread)
            { 
                return false;
            }
        }

        return true;
    }
}