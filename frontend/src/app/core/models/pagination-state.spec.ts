import { PaginationState } from './pagination-state';

describe('PaginationState', () => {
  it('defaults to ten items and suppresses duplicate page requests', () => {
    const state = new PaginationState();
    expect(state.pageNumber()).toBe(1);
    expect(state.pageSize()).toBe(10);
    expect(state.changePage(1)).toBe(false);
    expect(state.changePage(2)).toBe(true);
    expect(state.changePage(2)).toBe(false);
  });

  it('resets the page when changing size and keeps the preference across contexts', () => {
    const state = new PaginationState();
    state.changePage(3);
    expect(state.changePageSize(20)).toBe(true);
    expect(state.pageNumber()).toBe(1);
    expect(state.changePageSize(20)).toBe(false);
    state.acceptTotal(40);
    state.changePage(2);
    state.reset();
    expect(state.pageNumber()).toBe(1);
    expect(state.pageSize()).toBe(20);
    expect(state.totalCount()).toBe(0);
  });

  it('clamps a removed last page and handles an empty collection', () => {
    const state = new PaginationState();
    state.changePage(3);
    expect(state.acceptTotal(15)).toBe(true);
    expect(state.pageNumber()).toBe(2);
    expect(state.totalCount()).toBe(15);
    expect(state.acceptTotal(15)).toBe(false);
    expect(state.acceptTotal(0)).toBe(true);
    expect(state.pageNumber()).toBe(1);
  });

  it('rejects invalid bounds rather than silently producing an invalid query', () => {
    const state = new PaginationState();
    expect(() => state.changePage(0)).toThrow(RangeError);
    expect(() => state.changePageSize(101)).toThrow(RangeError);
    expect(() => state.acceptTotal(-1)).toThrow(RangeError);
  });
});
