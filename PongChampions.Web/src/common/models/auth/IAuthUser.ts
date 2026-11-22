export interface IAuthUser {
  userId: string;
  username: string;
  expiresAt: number;
}

export interface IRegistration {
  username: string;
  displayName: string;
  password: string;
  country?: string;
}
