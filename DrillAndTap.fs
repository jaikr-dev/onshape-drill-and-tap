FeatureScript 2931;
import(path : "onshape/std/common.fs", version : "2931.0");

// ---------------------------------------------------------------------
// Drill & Tap
// ---------------------------------------------------------------------
// Drills a tap-drill bore and cuts REAL helical thread geometry (not
// cosmetic) in a single feature, straight from a sketch point.
// Supports ISO metric and Unified (UNC/UNF) inch standards.
// Workflow:
//   1. Drill a cylindrical bore at the thread's minor diameter
//      (oversized if the through-hole option is on).
//   2. Optional lead-in chamfer at the entry edge.
//   3. Create a helix at the major diameter, with overhang on entry.
//   4. Sketch a 60 degree V profile on the radial plane through the
//      helix start point.
//   5. Sweep the V along the helix to make a "thread tool" body.
//   6. Boolean-subtract the tool from the target part(s).

export enum ThreadStandard
{
    annotation { "Name" : "Metric (ISO)" }       METRIC,
    annotation { "Name" : "Imperial (Unified)" } IMPERIAL,
    annotation { "Name" : "Custom" }             CUSTOM
}

export enum ThreadDiameter
{
    annotation { "Name" : "M3" }  M3,
    annotation { "Name" : "M4" }  M4,
    annotation { "Name" : "M5" }  M5,
    annotation { "Name" : "M6" }  M6,
    annotation { "Name" : "M8" }  M8,
    annotation { "Name" : "M10" } M10,
    annotation { "Name" : "M12" } M12,
    annotation { "Name" : "M14" } M14,
    annotation { "Name" : "M16" } M16,
    annotation { "Name" : "M20" } M20,
    annotation { "Name" : "M24" } M24
}

export enum ImperialDiameter
{
    annotation { "Name" : "#4" }     N4,
    annotation { "Name" : "#6" }     N6,
    annotation { "Name" : "#8" }     N8,
    annotation { "Name" : "#10" }    N10,
    annotation { "Name" : "1/4\"" }  IN_1_4,
    annotation { "Name" : "5/16\"" } IN_5_16,
    annotation { "Name" : "3/8\"" }  IN_3_8,
    annotation { "Name" : "7/16\"" } IN_7_16,
    annotation { "Name" : "1/2\"" }  IN_1_2,
    annotation { "Name" : "5/8\"" }  IN_5_8,
    annotation { "Name" : "3/4\"" }  IN_3_4,
    annotation { "Name" : "7/8\"" }  IN_7_8,
    annotation { "Name" : "1\"" }    IN_1
}

export enum PitchSeries
{
    annotation { "Name" : "Coarse" } COARSE,
    annotation { "Name" : "Fine" }   FINE
}

// ---------------------------------------------------------------------
// ISO 261 / DIN 13 metric pitch table.
// Coarse = default series. Fine = 1st-choice fine pitch.
// ---------------------------------------------------------------------
function lookupMetricThread(diam is ThreadDiameter, series is PitchSeries) returns map
{
    if (series == PitchSeries.COARSE)
    {
        if (diam == ThreadDiameter.M3)  return { "D" : 3   * millimeter, "P" : 0.5  * millimeter };
        if (diam == ThreadDiameter.M4)  return { "D" : 4   * millimeter, "P" : 0.7  * millimeter };
        if (diam == ThreadDiameter.M5)  return { "D" : 5   * millimeter, "P" : 0.8  * millimeter };
        if (diam == ThreadDiameter.M6)  return { "D" : 6   * millimeter, "P" : 1.0  * millimeter };
        if (diam == ThreadDiameter.M8)  return { "D" : 8   * millimeter, "P" : 1.25 * millimeter };
        if (diam == ThreadDiameter.M10) return { "D" : 10  * millimeter, "P" : 1.5  * millimeter };
        if (diam == ThreadDiameter.M12) return { "D" : 12  * millimeter, "P" : 1.75 * millimeter };
        if (diam == ThreadDiameter.M14) return { "D" : 14  * millimeter, "P" : 2.0  * millimeter };
        if (diam == ThreadDiameter.M16) return { "D" : 16  * millimeter, "P" : 2.0  * millimeter };
        if (diam == ThreadDiameter.M20) return { "D" : 20  * millimeter, "P" : 2.5  * millimeter };
        if (diam == ThreadDiameter.M24) return { "D" : 24  * millimeter, "P" : 3.0  * millimeter };
    }
    else
    {
        if (diam == ThreadDiameter.M3)  return { "D" : 3   * millimeter, "P" : 0.35 * millimeter };
        if (diam == ThreadDiameter.M4)  return { "D" : 4   * millimeter, "P" : 0.5  * millimeter };
        if (diam == ThreadDiameter.M5)  return { "D" : 5   * millimeter, "P" : 0.5  * millimeter };
        if (diam == ThreadDiameter.M6)  return { "D" : 6   * millimeter, "P" : 0.75 * millimeter };
        if (diam == ThreadDiameter.M8)  return { "D" : 8   * millimeter, "P" : 1.0  * millimeter };
        if (diam == ThreadDiameter.M10) return { "D" : 10  * millimeter, "P" : 1.25 * millimeter };
        if (diam == ThreadDiameter.M12) return { "D" : 12  * millimeter, "P" : 1.25 * millimeter };
        if (diam == ThreadDiameter.M14) return { "D" : 14  * millimeter, "P" : 1.5  * millimeter };
        if (diam == ThreadDiameter.M16) return { "D" : 16  * millimeter, "P" : 1.5  * millimeter };
        if (diam == ThreadDiameter.M20) return { "D" : 20  * millimeter, "P" : 1.5  * millimeter };
        if (diam == ThreadDiameter.M24) return { "D" : 24  * millimeter, "P" : 2.0  * millimeter };
    }
    return { "D" : 12 * millimeter, "P" : 1.75 * millimeter };
}

// ---------------------------------------------------------------------
// Unified inch thread table (ASME B1.1).
// Coarse = UNC. Fine = UNF.
// ---------------------------------------------------------------------
function lookupImperialThread(diam is ImperialDiameter, series is PitchSeries) returns map
{
    if (series == PitchSeries.COARSE)
    {
        if (diam == ImperialDiameter.N4)      return { "D" : 0.112  * inch, "P" : (1 / 40) * inch };
        if (diam == ImperialDiameter.N6)      return { "D" : 0.138  * inch, "P" : (1 / 32) * inch };
        if (diam == ImperialDiameter.N8)      return { "D" : 0.164  * inch, "P" : (1 / 32) * inch };
        if (diam == ImperialDiameter.N10)     return { "D" : 0.190  * inch, "P" : (1 / 24) * inch };
        if (diam == ImperialDiameter.IN_1_4)  return { "D" : 0.25   * inch, "P" : (1 / 20) * inch };
        if (diam == ImperialDiameter.IN_5_16) return { "D" : 0.3125 * inch, "P" : (1 / 18) * inch };
        if (diam == ImperialDiameter.IN_3_8)  return { "D" : 0.375  * inch, "P" : (1 / 16) * inch };
        if (diam == ImperialDiameter.IN_7_16) return { "D" : 0.4375 * inch, "P" : (1 / 14) * inch };
        if (diam == ImperialDiameter.IN_1_2)  return { "D" : 0.5    * inch, "P" : (1 / 13) * inch };
        if (diam == ImperialDiameter.IN_5_8)  return { "D" : 0.625  * inch, "P" : (1 / 11) * inch };
        if (diam == ImperialDiameter.IN_3_4)  return { "D" : 0.75   * inch, "P" : (1 / 10) * inch };
        if (diam == ImperialDiameter.IN_7_8)  return { "D" : 0.875  * inch, "P" : (1 / 9)  * inch };
        if (diam == ImperialDiameter.IN_1)    return { "D" : 1.0    * inch, "P" : (1 / 8)  * inch };
    }
    else
    {
        if (diam == ImperialDiameter.N4)      return { "D" : 0.112  * inch, "P" : (1 / 48) * inch };
        if (diam == ImperialDiameter.N6)      return { "D" : 0.138  * inch, "P" : (1 / 40) * inch };
        if (diam == ImperialDiameter.N8)      return { "D" : 0.164  * inch, "P" : (1 / 36) * inch };
        if (diam == ImperialDiameter.N10)     return { "D" : 0.190  * inch, "P" : (1 / 32) * inch };
        if (diam == ImperialDiameter.IN_1_4)  return { "D" : 0.25   * inch, "P" : (1 / 28) * inch };
        if (diam == ImperialDiameter.IN_5_16) return { "D" : 0.3125 * inch, "P" : (1 / 24) * inch };
        if (diam == ImperialDiameter.IN_3_8)  return { "D" : 0.375  * inch, "P" : (1 / 24) * inch };
        if (diam == ImperialDiameter.IN_7_16) return { "D" : 0.4375 * inch, "P" : (1 / 20) * inch };
        if (diam == ImperialDiameter.IN_1_2)  return { "D" : 0.5    * inch, "P" : (1 / 20) * inch };
        if (diam == ImperialDiameter.IN_5_8)  return { "D" : 0.625  * inch, "P" : (1 / 18) * inch };
        if (diam == ImperialDiameter.IN_3_4)  return { "D" : 0.75   * inch, "P" : (1 / 16) * inch };
        if (diam == ImperialDiameter.IN_7_8)  return { "D" : 0.875  * inch, "P" : (1 / 14) * inch };
        if (diam == ImperialDiameter.IN_1)    return { "D" : 1.0    * inch, "P" : (1 / 12) * inch };
    }
    return { "D" : 0.25 * inch, "P" : (1 / 20) * inch };
}

function perpVector(v is Vector) returns Vector
{
    var ref = vector(1, 0, 0);
    if (abs(v[0]) > 0.9)
        ref = vector(0, 1, 0);
    return normalize(cross(v, ref));
}

// Main feature

annotation { "Feature Type Name" : "Drill & Tap" }
export const drillAndTap = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        annotation { "Name" : "Sketch points",
                     "Filter" : EntityType.VERTEX && SketchObject.YES,
                     "MaxNumberOfPicks" : 1000 }
        definition.points is Query;

        annotation { "Name" : "Thread standard" }
        definition.threadStandard is ThreadStandard;

        if (definition.threadStandard == ThreadStandard.METRIC)
        {
            annotation { "Name" : "Diameter" }
            definition.metricDiam is ThreadDiameter;
        }
        else if (definition.threadStandard == ThreadStandard.IMPERIAL)
        {
            annotation { "Name" : "Diameter" }
            definition.imperialDiam is ImperialDiameter;
        }
        else
        {
            annotation { "Name" : "Major diameter (custom)" }
            isLength(definition.customMajor,
                     { (millimeter) : [0.5, 12, 500] } as LengthBoundSpec);

            annotation { "Name" : "Pitch (custom)" }
            isLength(definition.customPitch,
                     { (millimeter) : [0.1, 1.75, 20] } as LengthBoundSpec);
        }

        if (definition.threadStandard != ThreadStandard.CUSTOM)
        {
            annotation { "Name" : "Pitch series" }
            definition.pitchSeries is PitchSeries;
        }

        annotation { "Name" : "Through hole" }
        definition.throughHole is boolean;

        if (!definition.throughHole)
        {
            annotation { "Name" : "Hole depth" }
            isLength(definition.holeDepth,
                     { (millimeter) : [1, 20, 500] } as LengthBoundSpec);
        }

        annotation { "Name" : "Thread length" }
        isLength(definition.threadLength,
                 { (millimeter) : [1, 15, 500] } as LengthBoundSpec);

        annotation { "Name" : "Lead-in chamfer", "Default" : true }
        definition.addChamfer is boolean;

        if (definition.addChamfer)
        {
            annotation { "Name" : "Chamfer size" }
            isLength(definition.chamferSize,
                     { (millimeter) : [0.05, 0.5, 5] } as LengthBoundSpec);
        }

        annotation { "Name" : "Right-hand thread", "Default" : true }
        definition.rightHand is boolean;

        annotation { "Name" : "Flip direction",
                     "UIHint" : UIHint.OPPOSITE_DIRECTION }
        definition.flipDirection is boolean;

        annotation { "Name" : "Part(s) to thread",
                     "Filter" : EntityType.BODY && BodyType.SOLID,
                     "MaxNumberOfPicks" : 1000 }
        definition.targetBodies is Query;
    }
    {
        var thread;
        if (definition.threadStandard == ThreadStandard.METRIC)
            thread = lookupMetricThread(definition.metricDiam, definition.pitchSeries);
        else if (definition.threadStandard == ThreadStandard.IMPERIAL)
            thread = lookupImperialThread(definition.imperialDiam, definition.pitchSeries);
        else
            thread = { "D" : definition.customMajor, "P" : definition.customPitch };

        const majorD = thread.D;
        const pitch  = thread.P;
        const H      = pitch * sqrt(3) / 2;
        const minorD = majorD - 5 / 4 * H;

        if (minorD <= 0.1 * millimeter)
            throw regenError("Pitch is too large for the given major diameter.",
                             ["customPitch"]);

        // Through-hole: oversize the bore so it punches through any
        // reasonable plate. opBoolean SUBTRACTION consumes the whole
        // tool, so excessive depth costs nothing geometrically.
        var effectiveHoleDepth;
        if (definition.throughHole)
            effectiveHoleDepth = 1000 * millimeter;
        else
        {
            effectiveHoleDepth = definition.holeDepth;
            if (definition.threadLength > definition.holeDepth)
                throw regenError("Thread length must not exceed hole depth.",
                                 ["threadLength"]);
        }

        var chamferSize = 0 * millimeter;
        if (definition.addChamfer)
            chamferSize = definition.chamferSize;

        const pts = evaluateQuery(context, definition.points);
        if (size(pts) == 0)
            throw regenError("Select at least one sketch point.", ["points"]);

        const targets = evaluateQuery(context, definition.targetBodies);
        if (size(targets) == 0)
            throw regenError("Select at least one part to thread.",
                             ["targetBodies"]);

        // Body-center used to auto-detect drill direction per-point.
        const targetBox  = evBox3d(context, { "topology" : definition.targetBodies });
        const bodyCenter = (targetBox.minCorner + targetBox.maxCorner) * 0.5;

        for (var i = 0; i < size(pts); i += 1)
        {
            const ptQ   = pts[i];
            const ptPos = evVertexPoint(context, { "vertex" : ptQ });

            // Drill axis = sketch's plane normal. Sign chosen so the bore
            // heads toward the body, regardless of which way the sketch
            // normal happens to point.
            const skPlane = evOwnerSketchPlane(context, { "entity" : ptQ });
            const ptToBody = bodyCenter - ptPos;
            var axisDir = (dot(ptToBody, skPlane.normal) > 0)
                ? skPlane.normal
                : skPlane.normal * -1;
            if (definition.flipDirection)
                axisDir = axisDir * -1;

            buildThread(context, id + ("hole" ~ i), {
                "position"     : ptPos,
                "axis"         : normalize(axisDir),
                "majorD"       : majorD,
                "minorD"       : minorD,
                "pitch"        : pitch,
                "holeDepth"    : effectiveHoleDepth,
                "threadLength" : definition.threadLength,
                "rightHand"    : definition.rightHand,
                "targetBodies" : definition.targetBodies,
                "addChamfer"   : definition.addChamfer,
                "chamferSize"  : chamferSize
            });
        }
    });


// Per-hole construction: bore, optional chamfer, helix, V-sweep, subtract

function buildThread(context is Context, id is Id, p is map)
{
    const position  = p.position;
    const axis      = normalize(p.axis);
    const majorD    = p.majorD;
    const minorD    = p.minorD;
    const pitch     = p.pitch;
    const holeDepth = p.holeDepth;
    const threadLen = p.threadLength;

    const radial = perpVector(axis);

    // 1. Drill the bore at the minor diameter
    const drillSk = id + "drillSk";
    var ds = newSketchOnPlane(context, drillSk, {
        "sketchPlane" : plane(position, axis)
    });
    skCircle(ds, "drill", {
        "center" : vector(0, 0) * meter,
        "radius" : minorD / 2
    });
    skSolve(ds);

    opExtrude(context, id + "drillBody", {
        "entities"      : qSketchRegion(drillSk),
        "direction"     : axis,
        "endBound"      : BoundingType.BLIND,
        "endDepth"      : holeDepth,
        "operationType" : NewBodyOperationType.NEW
    });

    opBoolean(context, id + "drillSub", {
        "tools"         : qCreatedBy(id + "drillBody", EntityType.BODY),
        "targets"       : p.targetBodies,
        "operationType" : BooleanOperationType.SUBTRACTION
    });

    // 2. Optional lead-in chamfer at the entry
    if (p.addChamfer)
        buildChamfer(context, id + "ch", p, position, axis, radial, minorD);

    // 3. Helix path (with entry overhang)
    const topOver = pitch;
    const botOver = pitch * 0.25;
    const aStart  = position - axis * topOver;
    const hStart  = aStart + radial * (majorD / 2);
    const totalRevs = (threadLen + topOver + botOver) / pitch;

    const helixId = id + "helix";
    opHelix(context, helixId, {
        "direction"    : axis,
        "axisStart"    : aStart,
        "startPoint"   : hStart,
        "interval"     : [0, totalRevs],
        "clockwise"    : !p.rightHand,
        "helicalPitch" : pitch,
        "spiralPitch"  : 0 * meter
    });

    // 4. 60 degree V profile on the radial plane
    const grooveDepth = (majorD - minorD) / 2;
    const eps         = pitch * 0.05;
    const baseY       = -(grooveDepth + eps);
    const baseHalfX   = (grooveDepth + eps) * tan(30 * degree);

    const profNormal = cross(axis, radial);
    const profPlane  = plane(hStart, profNormal, axis);

    const profSk = id + "profSk";
    var ps = newSketchOnPlane(context, profSk, { "sketchPlane" : profPlane });
    skLineSegment(ps, "vTop", {
        "start" : vector(0 * meter, 0 * meter),
        "end"   : vector(baseHalfX, baseY)
    });
    skLineSegment(ps, "vBot", {
        "start" : vector(0 * meter, 0 * meter),
        "end"   : vector(-baseHalfX, baseY)
    });
    skLineSegment(ps, "vBase", {
        "start" : vector(baseHalfX, baseY),
        "end"   : vector(-baseHalfX, baseY)
    });
    skSolve(ps);

    // 5. Sweep V along helix -> thread tool body
    const sweepId = id + "sweep";
    opSweep(context, sweepId, {
        "profiles" : qSketchRegion(profSk),
        "path"     : qCreatedBy(helixId, EntityType.EDGE)
    });

    // 6. Subtract the tool body from target part(s)
    opBoolean(context, id + "sub", {
        "tools"         : qCreatedBy(sweepId, EntityType.BODY),
        "targets"       : p.targetBodies,
        "operationType" : BooleanOperationType.SUBTRACTION
    });

    // 7. Clean up helper helix wire body
    opDeleteBodies(context, id + "delHelix", {
        "entities" : qCreatedBy(helixId, EntityType.BODY)
    });
}

// Lead-in chamfer: revolve a right-triangle profile around the hole
// axis and subtract the resulting frustum from the target part.

function buildChamfer(context is Context, id is Id, p is map,
                      position is Vector, axis is Vector, radial is Vector,
                      minorD)
{
    const chamferSize = p.chamferSize;
    const eps = chamferSize * 0.05;

    const profNormal = cross(axis, radial);
    const chPlane    = plane(position, profNormal, axis);

    const chSk = id + "chSk";
    var cs = newSketchOnPlane(context, chSk, { "sketchPlane" : chPlane });

    // Sketch coords: +x = axial direction (into body), +y = radial.
    // v1: just outside entry face, slight overlap inside the bore radius
    // v2: just outside entry face, at outer chamfer radius
    // v3: inside the body by chamferSize, at the bore wall (chamfer apex)
    const v1 = vector(-eps, minorD / 2 - eps);
    const v2 = vector(-eps, minorD / 2 + chamferSize);
    const v3 = vector(chamferSize, minorD / 2 - eps);

    skLineSegment(cs, "e1", { "start" : v1, "end" : v2 });
    skLineSegment(cs, "e2", { "start" : v2, "end" : v3 });
    skLineSegment(cs, "e3", { "start" : v3, "end" : v1 });
    skSolve(cs);

    // Revolve a full 360 degrees around the hole axis to make a frustum.
    opRevolve(context, id + "rev", {
        "entities"     : qSketchRegion(chSk),
        "axis"         : line(position, axis),
        "angleForward" : 360 * degree
    });

    // Subtract the frustum from the target part(s).
    opBoolean(context, id + "sub", {
        "tools"         : qCreatedBy(id + "rev", EntityType.BODY),
        "targets"       : p.targetBodies,
        "operationType" : BooleanOperationType.SUBTRACTION
    });
}
