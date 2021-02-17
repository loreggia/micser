import React, { FC } from "react";
import { EdgeProps, getBezierPath, getMarkerEnd } from "react-flow-renderer";

export const CustomEdge: FC<EdgeProps> = ({
    id,
    sourceX,
    sourceY,
    targetX,
    targetY,
    sourcePosition,
    targetPosition,
    style = {},
    label,
    arrowHeadType,
    markerEndId,
    selected,
}) => {
    const edgePath = getBezierPath({ sourceX, sourceY, sourcePosition, targetX, targetY, targetPosition });
    const markerEnd = getMarkerEnd(arrowHeadType, markerEndId);
    style.cursor = "pointer";
    return (
        <>
            <path id={id} style={style} className="react-flow__edge-path" d={edgePath} markerEnd={markerEnd} />
            {label && (
                <text dy="-3px">
                    <textPath
                        href={`#${id}`}
                        style={{ fill: "white", cursor: "pointer" }}
                        startOffset="50%"
                        textAnchor="middle"
                    >
                        {label}
                    </textPath>
                </text>
            )}
        </>
    );
};
