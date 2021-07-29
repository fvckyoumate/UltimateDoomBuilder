`#name Delete Sector Tag`;

`#description Deletes a tag from the selected sectors (or all sectors if no sectors are selected)`;

let sectors = Map.getSelectedSectors();

if(sectors.length == 0)
    sectors = Map.getSectors();

let qo = new QueryOptions();
qo.addOption('tag', 'Tag to delete', 1, 0);
if(!qo.query())
    throw 'Script aborted';

if(qo.options.tag == 0)
    throw "Tag can't be 0";

sectors.forEach(s => s.removeTag(qo.options.tag));