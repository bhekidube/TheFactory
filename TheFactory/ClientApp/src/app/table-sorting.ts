import { Pipe, PipeTransform } from '@angular/core';

export type TableSortDirection = 'asc' | 'desc';

interface GridSortState {
  column: string;
  direction: TableSortDirection;
}

export class TableSortController {
  sortColumn = '';
  sortDirection: TableSortDirection = 'asc';
  private readonly gridSortStates: { [grid: string]: GridSortState } = {};

  sortGrid(column: string, grid = 'default'): void {
    const current = grid === 'default'
      ? { column: this.sortColumn, direction: this.sortDirection }
      : this.gridSortStates[grid] || { column: '', direction: 'asc' as TableSortDirection };
    const direction: TableSortDirection = current.column === column
      ? current.direction === 'asc' ? 'desc' : 'asc'
      : 'asc';
    const next = { column, direction };

    if (grid === 'default') {
      this.sortColumn = next.column;
      this.sortDirection = next.direction;
    } else {
      this.gridSortStates[grid] = next;
    }
  }

  getGridSortColumn(grid = 'default'): string {
    return grid === 'default' ? this.sortColumn : this.gridSortStates[grid]?.column || '';
  }

  getGridSortDirection(grid = 'default'): TableSortDirection {
    return grid === 'default' ? this.sortDirection : this.gridSortStates[grid]?.direction || 'asc';
  }

  getAriaSort(column: string, grid = 'default'): 'ascending' | 'descending' | 'none' {
    if (this.getGridSortColumn(grid) !== column) {
      return 'none';
    }
    return this.getGridSortDirection(grid) === 'asc' ? 'ascending' : 'descending';
  }

  getSortIndicator(column: string, grid = 'default'): string {
    return this.getGridSortColumn(grid) !== column
      ? '↕'
      : this.getGridSortDirection(grid) === 'asc' ? '▲' : '▼';
  }
}

@Pipe({ name: 'tableSort' })
export class TableSortPipe implements PipeTransform {
  transform<T>(
    rows: readonly T[] | null | undefined,
    column: string,
    direction: TableSortDirection = 'asc'
  ): T[] {
    if (!rows || !column) {
      return rows ? [...rows] : [];
    }

    const collator = new Intl.Collator(undefined, {
      numeric: true,
      sensitivity: 'base'
    });
    const multiplier = direction === 'asc' ? 1 : -1;

    return [...rows].sort((left, right) => {
      const leftValue = getSortValue(left, column);
      const rightValue = getSortValue(right, column);
      const leftEmpty = isEmptySortValue(leftValue);
      const rightEmpty = isEmptySortValue(rightValue);

      if (leftEmpty || rightEmpty) {
        return leftEmpty === rightEmpty ? 0 : leftEmpty ? 1 : -1;
      }

      const comparison = compareSortValues(leftValue, rightValue, column, collator);
      return comparison * multiplier;
    });
  }
}

function getSortValue(row: unknown, column: string): unknown {
  if (row && typeof row === 'object') {
    const record = row as Record<string, unknown>;
    if (column === 'learnerName') {
      return `${String(record['firstName'] || '')} ${String(record['surname'] || '')}`.trim();
    }
    if (column === 'guardianName') {
      const guardian = record['parentGuardian'] as Record<string, unknown> | null;
      return guardian
        ? `${String(guardian['firstName'] || '')} ${String(guardian['surname'] || '')}`.trim()
        : '';
    }
    if (column === 'referenceNumber') {
      return record['referenceNumber'] || record['category'] || record['notes'];
    }
    if (column === 'percentage' && typeof record['possibleMark'] === 'number') {
      const possibleMark = record['possibleMark'];
      return possibleMark > 0 ? Number(record['pupilMark'] || 0) / possibleMark * 100 : 0;
    }
    if (column === 'debitAmount' || column === 'creditAmount') {
      const activeEntry = !record['isVoided'];
      const matchesType = column === 'debitAmount'
        ? record['type'] === 'DEBIT'
        : record['type'] === 'CREDIT';
      return activeEntry && matchesType ? record['amount'] : null;
    }
    if (column === 'termYear') {
      return `${String(record['term'] || '')} ${String(record['academicYear'] || '')}`.trim();
    }
    if (column === 'departureDate' || column === 'departureTime') {
      const departureDateTime = record['departureDateTime'];
      if (typeof departureDateTime === 'string') {
        const parsed = new Date(departureDateTime);
        if (!Number.isNaN(parsed.getTime())) {
          return column === 'departureDate' ? parsed.toISOString().slice(0, 10) : parsed.toISOString().slice(11, 19);
        }
      }
    }
  }

  return column.split('.').reduce<unknown>((value, key) => {
    if (value === null || typeof value !== 'object') {
      return undefined;
    }
    return (value as Record<string, unknown>)[key];
  }, row);
}

function isEmptySortValue(value: unknown): boolean {
  if (value === null || value === undefined) {
    return true;
  }
  return typeof value === 'string'
    && (!value.trim() || value.trim() === '-' || value.trim() === '—');
}

function compareSortValues(
  left: unknown,
  right: unknown,
  column: string,
  collator: Intl.Collator
): number {
  if (typeof left === 'number' && typeof right === 'number') {
    return left - right;
  }
  if (typeof left === 'boolean' && typeof right === 'boolean') {
    return Number(left) - Number(right);
  }
  if (left instanceof Date && right instanceof Date) {
    return left.getTime() - right.getTime();
  }

  const leftText = stringifySortValue(left);
  const rightText = stringifySortValue(right);

  if (isDateColumn(column)) {
    const leftDate = Date.parse(leftText);
    const rightDate = Date.parse(rightText);
    if (!Number.isNaN(leftDate) && !Number.isNaN(rightDate)) {
      return leftDate - rightDate;
    }
  }

  if (isNumericColumn(column)) {
    const leftNumber = Number(leftText.match(/-?\d+(?:\.\d+)?/)?.[0]);
    const rightNumber = Number(rightText.match(/-?\d+(?:\.\d+)?/)?.[0]);
    if (Number.isFinite(leftNumber) && Number.isFinite(rightNumber)) {
      return leftNumber - rightNumber;
    }
  }

  return collator.compare(leftText, rightText);
}

function stringifySortValue(value: unknown): string {
  if (value && typeof value === 'object' && !(value instanceof Date)) {
    return Object.values(value as Record<string, unknown>)
      .map(item => stringifySortValue(item))
      .filter(Boolean)
      .join(' ');
  }
  return value instanceof Date ? value.toISOString() : String(value ?? '');
}

function isDateColumn(column: string): boolean {
  return /date|time|created|updated|due|birth/i.test(column);
}

function isNumericColumn(column: string): boolean {
  return /(^id$|id$|amount|price|mark|balance|year|count|length|quantity|score|percentage|temperature|debit|credit)/i.test(column);
}
