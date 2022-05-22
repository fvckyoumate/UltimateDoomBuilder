const selectedLinedefs = UDB.Map.getSelectedLinedefs();
const unSelectedLinedefs = UDB.Map.getLinedefs().filter(ld => !selectedLinedefs.includes(ld));
let startPosition = null;
let selecting = false;
let vertices = new Set();
let bevels = {};
let lines = {};

// Collect all vertices that have exactly 2 linedefs and both of those are selected
selectedLinedefs.forEach(ld => [ ld.start, ld.end ].forEach(v => {
    let vertexlines = v.getLinedefs();

    if(vertexlines.length == 2 && vertexlines.every(ld2 => ld2.selected))
        vertices.add(v);
    })
);

updateLines();

function updateLines()
{
    let length = startPosition == null ? 0 : UDB.Map.snappedToGrid([ new UDB.Line2D(startPosition, UDB.Map.mousePosition).getLength(), 0 ]).x;

    // Go through all collected vertices
    vertices.forEach(v => {
        bevels[v] = [];

        // Split all lines at the given size from the vertex away
        v.getLinedefs().forEach(ld => {
            if(!(ld in lines))
                lines[ld] = [ ld.start.position, ld.end.position ];

            if(ld.start == v)
            {
                lines[ld][0] = ld.line.getCoordinatesAt(1.0 / ld.length * length);
                bevels[v].push({ ld: ld, pos: 0 });
            }
            else
            {
                lines[ld][1] = ld.line.getCoordinatesAt(1.0 - (1.0 / ld.length * length));
                bevels[v].push({ ld: ld, pos: 1 });
            }
        });
    });
}

function onSelectBegin()
{
    startPosition = UDB.Map.mousePosition;
    selecting = true;
}

function onSelectEnd()
{
    selecting = false;
    UDB.Mode.redraw();
    UDB.Mode.accept();
}

function onMouseMove()
{
    if(selecting)
    {
        updateLines();

        UDB.Mode.redraw();
    }
}

function onRedrawDisplay()
{
    UDB.Mode.plotLinedefSet(unSelectedLinedefs);

    for(const ld in lines)
        UDB.Mode.plotLine(lines[ld][0], lines[ld][1], 255, 0, 0);

    for(const v in bevels)
        UDB.Mode.plotLine(lines[bevels[v][0].ld][bevels[v][0].pos], lines[bevels[v][1].ld][bevels[v][1].pos], 255, 0, 255);

    if(selecting)
        UDB.Mode.plotLine(startPosition, UDB.Map.mousePosition, 255, 255, 0);
}

function onAccept()
{
    let length = UDB.Map.snappedToGrid([ new UDB.Line2D(startPosition, UDB.Map.mousePosition).getLength(), 0 ]).x;

    // Go through all collected vertices
    vertices.forEach(v => {
        // Split all lines at the given size from the vertex away
        v.getLinedefs().forEach(ld => {
            if(ld.start == v)
                ld.split(ld.line.getCoordinatesAt(1.0 / ld.length * length));
            else
                ld.split(ld.line.getCoordinatesAt(1.0 - (1.0 / ld.length * length)));
        });

        // Get one of the connected linedef...
        let ld = v.getLinedefs()[0];

        // ... and join the current vertex into the linedef's closer vertex
        if(ld.start == v)
            v.join(ld.end);
        else
            v.join(ld.start);
    });
}