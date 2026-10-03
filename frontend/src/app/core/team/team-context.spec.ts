import { TestBed } from '@angular/core/testing';

import { TeamContext } from './team-context';

const SELECTED_TEAM_STORAGE_KEY = 'selectedTeamId';

describe('TeamContext', () => {
  beforeEach(() => {
    localStorage.removeItem(SELECTED_TEAM_STORAGE_KEY);
  });

  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.removeItem(SELECTED_TEAM_STORAGE_KEY);
  });

  it('restores and persists the selected team', () => {
    localStorage.setItem(SELECTED_TEAM_STORAGE_KEY, 'team-1');
    const context = TestBed.inject(TeamContext);

    expect(context.selectedTeamId()).toBe('team-1');
    expect(context.hasSelectedTeam()).toBe(true);

    context.selectTeam('team-2');
    expect(localStorage.getItem(SELECTED_TEAM_STORAGE_KEY)).toBe('team-2');

    context.clearTeam();
    expect(context.selectedTeamId()).toBeNull();
    expect(context.hasSelectedTeam()).toBe(false);
    expect(localStorage.getItem(SELECTED_TEAM_STORAGE_KEY)).toBeNull();
  });

  it('increments synchronously on changes even when returning to the original team', () => {
    const context = TestBed.inject(TeamContext);
    context.selectTeam('a');
    expect(context.selectionVersion()).toBe(1);
    context.selectTeam('b');
    context.selectTeam('a');
    expect(context.selectedTeamId()).toBe('a');
    expect(context.selectionVersion()).toBe(3);
    expect(localStorage.getItem(SELECTED_TEAM_STORAGE_KEY)).toBe('a');
  });

  it('does not advance the revision when selecting the same team', () => {
    const context = TestBed.inject(TeamContext);
    context.selectTeam('a');
    context.selectTeam('a');
    expect(context.selectionVersion()).toBe(1);
  });

  it('advances the revision on clearing a selection but not an already empty selection', () => {
    const context = TestBed.inject(TeamContext);
    context.selectTeam('a');
    context.clearTeam();
    context.clearTeam();
    expect(context.selectionVersion()).toBe(2);
    expect(context.hasSelectedTeam()).toBe(false);
    expect(localStorage.getItem(SELECTED_TEAM_STORAGE_KEY)).toBeNull();
  });

  it('remains usable when browser storage is blocked', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new DOMException('Storage is blocked', 'SecurityError');
    });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('Storage is blocked', 'SecurityError');
    });
    vi.spyOn(Storage.prototype, 'removeItem').mockImplementation(() => {
      throw new DOMException('Storage is blocked', 'SecurityError');
    });
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {});
    const context = TestBed.inject(TeamContext);

    expect(context.selectedTeamId()).toBeNull();
    context.selectTeam('team-1');
    expect(context.selectedTeamId()).toBe('team-1');
    context.clearTeam();
    expect(context.selectedTeamId()).toBeNull();
    expect(warn).toHaveBeenCalledTimes(3);
  });

  it('propagates storage errors other than browser access restrictions', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('Unexpected storage failure');
    });

    expect(() => TestBed.inject(TeamContext)).toThrow('Unexpected storage failure');
  });
});
