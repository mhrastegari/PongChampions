import { describe, expect, it } from 'vitest';
import type { IRoomDto } from '@services/_api';
import { roomMapper, roomStatusMapper } from './roomMapper';

const dto: IRoomDto = {
  id: '1',
  createdAt: '2026-11-22T10:00:00Z',
  code: 'ABC123',
  hostPlayerId: 'host',
  hostDisplayName: 'Ada',
  status: 'WaitingForPlayers',
};

describe('roomMapper', () => {
  it('accepts the HTTP enum name and the SignalR enum number', () => {
    expect(roomStatusMapper('InGame')).toBe('InGame');
    expect(roomStatusMapper(2)).toBe('InGame');
    expect(roomStatusMapper(4)).toBe('Cancelled');
  });

  it('rejects statuses the client does not know', () => {
    expect(() => roomStatusMapper(9)).toThrow();
    expect(() => roomStatusMapper('Paused')).toThrow();
  });

  it('fills optional fields with explicit defaults', () => {
    expect(roomMapper(dto)).toMatchObject({
      guestPlayerId: null,
      guestDisplayName: null,
      targetScore: 5,
    });
    expect(roomMapper({ ...dto, maxScore: 11 }).targetScore).toBe(11);
  });
});
