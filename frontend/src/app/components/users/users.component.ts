import { Component, TemplateRef, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { BsModalRef, BsModalService } from 'ngx-bootstrap/modal';
import { Connection } from 'src/app/models/connection';
import { User } from 'src/app/models/entity/User';
import { UserRoleType } from 'src/app/models/enum/UserRoleType';
import { AuthenticationService } from 'src/app/services/authentication.service';
import { EnvironmentService } from 'src/app/services/environment.service';
import { UsersService } from 'src/app/services/users.service';

@Component({
  selector: 'app-users',
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.scss']
})
export class UsersComponent {
  UserRoleType = UserRoleType;

  users: User[] = [];
  connection?: Connection;

  currentUser?: User;
  isNewUser: boolean = false;
  isPasswordVisible: boolean = false;

  availableRoles = [UserRoleType.USER];

  @ViewChild('userEditorTemplate') userEditorTemplate?: TemplateRef<any>;
	userEditorModalRef?: BsModalRef;


  constructor(
    private router: Router,
    private environmentService: EnvironmentService,
    private authenticationService: AuthenticationService,
    private usersService: UsersService,
    private modalService: BsModalService,
  ) {
    this.load();
  }

  private async load(): Promise<void> {
    while (!this.environmentService.isReady()) {
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    await new Promise(resolve => setTimeout(resolve, 300));
    if (!this.authenticationService.connection) {
      this.router.navigate(['/login']);
      return;
    }
    this.connection = this.authenticationService.connection;
    if (this.connection.roleType != UserRoleType.ADMIN) {
      this.router.navigate(['/devices']);
      return;
    }
    this.loadUsers();
  }

  private async loadUsers(): Promise<void> {
    this.users = await this.usersService.getList();
  }

  showUserEditor(user: User) {
		if (user == null)
			return;

		this.currentUser = { ...user };

		this.userEditorModalRef = this.modalService.show(this.userEditorTemplate!, { class: 'modal-md' });
	}

  closeUserEditor() {
    this.userEditorModalRef?.hide();
    this.currentUser = undefined;
  }

	editUser(username: string) {
		const user = this.users.find(t => t.username == username);
		if (user == null)
			return;

		this.isNewUser = false;
		this.showUserEditor(user);
	}

	createUser() {
		const user: User = {
      username: '',
      password: '',
			roleType: UserRoleType.USER,
		};
		this.isNewUser = true;
		this.showUserEditor(user);
	}

  togglePasswordVisibility() {
    this.isPasswordVisible = !this.isPasswordVisible;
  }

  async submitUserEditor() {
    if (!this.currentUser)
      return;

    if (this.isNewUser) {
      await this.usersService.create(this.currentUser);
    } else {
      await this.usersService.update(this.currentUser);
    }

    this.loadUsers();
    this.closeUserEditor();
  }

  async deleteUser(username: string) {
    if (!confirm(`Are you sure you want to delete user "${username}"?`)) {
      return;
    }

    await this.usersService.delete(username);
    this.loadUsers();
  }

}
