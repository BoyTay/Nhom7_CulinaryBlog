/**
 * The API keeps ingredients and steps in creation order and has no reorder endpoint. To save a new order, the rows that
 * still exist keep their saved order of ids and take the content of whichever row now sits at that position; extra rows
 * become new records, which the server appends at the end. Every row therefore lands where the author put it.
 */
export function assignPersistedIds<Row extends { id?: string }>(rows: readonly Row[], persisted: readonly Row[]): Row[] {
  const keptIds = new Set(rows.flatMap((row) => (row.id ? [row.id] : [])));
  const ids = persisted.flatMap((row) => (row.id && keptIds.has(row.id) ? [row.id] : []));
  return rows.map((row, index) => ({ ...row, id: ids[index] }));
}
