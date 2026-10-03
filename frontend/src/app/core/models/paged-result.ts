export interface PagedResult<T> {
  items: readonly T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}
