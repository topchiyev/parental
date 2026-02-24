import { UserRoleType } from "./enum/UserRoleType";

export interface Connection {
  username: string;
  token: string;
  roleType: UserRoleType;
}
