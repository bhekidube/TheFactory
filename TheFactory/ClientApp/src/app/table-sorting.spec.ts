import { TableSortController, TableSortPipe } from './table-sorting';

describe('TableSortController', () => {
  it('toggles the selected column direction and resets direction for a new column', () => {
    const controller = new TableSortController();

    controller.sortGrid('name');
    expect(controller.getGridSortDirection()).toBe('asc');

    controller.sortGrid('name');
    expect(controller.getGridSortDirection()).toBe('desc');

    controller.sortGrid('id');
    expect(controller.getGridSortDirection()).toBe('asc');
  });

  it('keeps sort state independent for separate grids', () => {
    const controller = new TableSortController();

    controller.sortGrid('name', 'learners');
    controller.sortGrid('date', 'reports');

    expect(controller.getGridSortColumn('learners')).toBe('name');
    expect(controller.getGridSortColumn('reports')).toBe('date');
    expect(controller.getGridSortColumn()).toBe('');
  });
});

describe('TableSortPipe', () => {
  const pipe = new TableSortPipe();

  it('sorts numeric identifiers and leaves empty values at the bottom in either direction', () => {
    const rows = [{ id: 10 }, { id: 2 }, { id: null }, { id: 1 }];

    expect(pipe.transform(rows, 'id', 'asc').map(row => row.id)).toEqual([1, 2, 10, null]);
    expect(pipe.transform(rows, 'id', 'desc').map(row => row.id)).toEqual([10, 2, 1, null]);
    expect(rows.map(row => row.id)).toEqual([10, 2, null, 1]);
  });

  it('sorts nested names without moving dash placeholders above real values', () => {
    const rows = [{ parentGuardian: { surname: 'Zulu' } }, { parentGuardian: null }, { parentGuardian: { surname: 'Alpha' } }];

    expect(pipe.transform(rows, 'guardianName', 'asc')).toEqual([
      rows[2],
      rows[0],
      rows[1]
    ]);
  });
});
