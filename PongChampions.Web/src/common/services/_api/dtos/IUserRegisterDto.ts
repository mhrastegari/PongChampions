export interface IUserRegisterDto {
  username: string;
  password: string;
  displayName: string;
  country?: string | null;
}
