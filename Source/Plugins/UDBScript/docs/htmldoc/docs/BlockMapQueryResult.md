# BlockMapQueryResult

A `BlockMapQueryResult` is an object returned by the `getLineBlocks` and `getRectangleBlocks` methods of the `BlockMap` class. It has methods It has methods to retrieve the linedefs, things, sectors, and vertices that are in the queried blocks. The object is also iterable, returning each block, in cases where more fine-grained control is needed.

```js
const blockmap = new UDB.BlockMap();
const result = blockmap.getLineBlocks([ 0, 0 ], [ 512, 256 ]);

// Print all linedefs in the blocks
result.getLinedefs().forEach(ld => UDB.log(ld));
```
Looping over each block:

```js
const blockmap = new UDB.BlockMap();
const result = blockmap.getLineBlocks([ 0, 0 ], [ 512, 256 ]);

for(const block of result)
{
	UDB.log('--- New block ---');
	block.getLinedefs().forEach(ld => UDB.log(ld));
}
```
!!! note
    The methods to retrieve map elements from `BlockMapQueryResult` return arrays that only contain each map element once, since linedefs and sectors can be in multiple blocks, looping over a `BlockMapQueryResult` using `for...of` can return the same map elements multiple times.
## Methods

---
### getLinedefs()
Gets all `Linedef`s in the blockmap query result.
#### Return value
`Array` of `Linedef`s

---
### getSectors()
Gets all `Sector`s in the blockmap query result.
#### Return value
`Array` of `Sector`s

---
### getThings()
Gets all `Thing`s in the blockmap query result.
#### Return value
`Array` of `Thing`s

---
### getVertices()
Gets all `Vertex` in the blockmap query result.
#### Return value
`Array` of `Vertex`
