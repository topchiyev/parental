import { UserRoleType } from "../enum/UserRoleType";

export interface User {
  username: string;
  password: string;
  roleType: UserRoleType;
}
