import { signal } from '@angular/core';

export class PaginationState {
  readonly pageNumber = signal(1);
  readonly pageSize = signal(10);
  readonly totalCount = signal(0);

  changePage(page: number): boolean {
    if (!Number.isSafeInteger(page) || page < 1) {
      throw new RangeError('Page number must be a positive integer.');
    }
    if (page === this.pageNumber()) {
      return false;
    }
    this.pageNumber.set(page);
    return true;
  }

  changePageSize(size: number): boolean {
    if (!Number.isSafeInteger(size) || size < 1 || size > 100) {
      throw new RangeError('Page size must be between 1 and 100.');
    }
    if (size === this.pageSize()) {
      return false;
    }
    this.pageSize.set(size);
    this.pageNumber.set(1);
    return true;
  }

  reset(): void {
    this.pageNumber.set(1);
    this.totalCount.set(0);
  }

  acceptTotal(total: number): boolean {
    if (!Number.isSafeInteger(total) || total < 0) {
      throw new RangeError('Total count must be a non-negative integer.');
    }
    this.totalCount.set(total);
    const lastPage = Math.max(1, Math.ceil(total / this.pageSize()));
    if (this.pageNumber() > lastPage) {
      this.pageNumber.set(lastPage);
      return true;
    }
    return false;
  }
}
