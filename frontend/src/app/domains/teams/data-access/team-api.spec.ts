import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '@environments/environment';
import { CreateTeamRequest, Team, UpdateTeamRequest } from '../models/team';
import {
  CreateTeamMemberRequest,
  TeamMember,
  UpdateTeamMemberRequest,
} from '../models/team-member';
import { TeamApi } from './team-api';

describe('TeamApi', () => {
  let api: TeamApi;
  let http: HttpTestingController;
  const team: Team = {
    id: 'team-id',
    ownerUserId: 'owner-id',
    name: 'Platform',
    description: null,
    createdAt: '2026-10-01T00:00:00Z',
  };
  const member: TeamMember = {
    id: 'member-id',
    teamId: team.id,
    name: 'Example member',
    email: 'member@example.com',
    role: 'Developer',
    providerUserId: null,
    createdAt: '2026-10-01T00:00:00Z',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(TeamApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads all teams for selection and initialization without pagination', () => {
    api.getTeams().subscribe((teams) => expect(teams).toEqual([team]));
    const request = http.expectOne(`${environment.apiUrl}/teams`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.keys()).toEqual([]);
    request.flush([team]);
  });

  it('loads one team including its owner', () => {
    api.getTeam(team.id).subscribe((response) => expect(response).toEqual(team));
    const request = http.expectOne(`${environment.apiUrl}/teams/${team.id}`);
    expect(request.request.method).toBe('GET');
    request.flush(team);
  });

  it('creates a team with the supplied request', () => {
    const body: CreateTeamRequest = { name: team.name, description: null };
    api.createTeam(body).subscribe((response) => expect(response).toEqual(team));
    const request = http.expectOne(`${environment.apiUrl}/teams/`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush(team);
  });

  it('updates a team with the supplied request', () => {
    const body: UpdateTeamRequest = { name: 'Updated team', description: 'Description' };
    const updatedTeam: Team = { ...team, ...body };
    api.updateTeam(team.id, body).subscribe((response) => expect(response).toEqual(updatedTeam));
    const request = http.expectOne(`${environment.apiUrl}/teams/${team.id}`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(body);
    request.flush(updatedTeam);
  });

  it('deletes a team', () => {
    const next = vi.fn();
    api.deleteTeam(team.id).subscribe(next);
    const request = http.expectOne(`${environment.apiUrl}/teams/${team.id}`);
    expect(request.request.method).toBe('DELETE');
    request.flush(null, { status: 204, statusText: 'No Content' });
    expect(next).toHaveBeenCalledWith(null);
  });

  it('loads all members without pagination', () => {
    api.getMembers(team.id).subscribe((members) => expect(members).toEqual([member]));
    const request = http.expectOne(`${environment.apiUrl}/teams/${team.id}/members/`);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.keys()).toEqual([]);
    request.flush([member]);
  });

  it('loads one member from the selected team', () => {
    api.getMember(team.id, member.id).subscribe((response) => expect(response).toEqual(member));
    const request = http.expectOne(`${environment.apiUrl}/teams/${team.id}/members/${member.id}`);
    expect(request.request.method).toBe('GET');
    request.flush(member);
  });

  it('creates a member with the supplied request', () => {
    const body: CreateTeamMemberRequest = {
      name: member.name,
      email: member.email,
      role: member.role,
      providerUserId: null,
    };
    api.createMember(team.id, body).subscribe((response) => expect(response).toEqual(member));
    const request = http.expectOne(`${environment.apiUrl}/teams/${team.id}/members/`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    request.flush(member);
  });

  it('updates a member with the supplied request', () => {
    const body: UpdateTeamMemberRequest = {
      name: 'Updated member',
      email: member.email,
      role: 'TechLead',
      providerUserId: 'provider-id',
    };
    const updatedMember: TeamMember = { ...member, ...body };
    api
      .updateMember(team.id, member.id, body)
      .subscribe((response) => expect(response).toEqual(updatedMember));
    const request = http.expectOne(`${environment.apiUrl}/teams/${team.id}/members/${member.id}`);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(body);
    request.flush(updatedMember);
  });

  it('deletes a member from the selected team', () => {
    const next = vi.fn();
    api.deleteMember(team.id, member.id).subscribe(next);
    const request = http.expectOne(`${environment.apiUrl}/teams/${team.id}/members/${member.id}`);
    expect(request.request.method).toBe('DELETE');
    request.flush(null, { status: 204, statusText: 'No Content' });
    expect(next).toHaveBeenCalledWith(null);
  });

  it('requests a bounded teams page with encoded search and retains metadata', () => {
    api.getTeamsPage(2, 20, ' Platform & API + Tools? ').subscribe((page) => {
      expect(page).toEqual({ items: [team], totalCount: 35, pageNumber: 2, pageSize: 20 });
    });
    const request = http.expectOne(
      (candidate) => candidate.url === `${environment.apiUrl}/teams/paged`,
    );
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('pageNumber')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('20');
    expect(request.request.params.get('search')).toBe('Platform & API + Tools?');
    expect(request.request.urlWithParams).toContain(
      'search=Platform%20%26%20API%20%2B%20Tools?',
    );
    request.flush({ items: [team], totalCount: 35, pageNumber: 2, pageSize: 20 });
  });

  it.each([undefined, '', ' '])('omits blank or absent team searches (%s)', (search) => {
    api.getTeamsPage(1, 10, search).subscribe();
    const request = http.expectOne(
      (candidate) => candidate.url === `${environment.apiUrl}/teams/paged`,
    );
    expect(request.request.params.has('search')).toBe(false);
    request.flush({ items: [], totalCount: 0, pageNumber: 1, pageSize: 10 });
  });

  it('requests only the selected team member page and retains full total', () => {
    api.getMembersPage(team.id, 3, 10).subscribe((page) => {
      expect(page).toEqual({ items: [member], totalCount: 24, pageNumber: 3, pageSize: 10 });
    });
    const request = http.expectOne(
      (candidate) => candidate.url === `${environment.apiUrl}/teams/${team.id}/members/paged`,
    );
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('pageNumber')).toBe('3');
    expect(request.request.params.get('pageSize')).toBe('10');
    request.flush({ items: [member], totalCount: 24, pageNumber: 3, pageSize: 10 });
  });
});
