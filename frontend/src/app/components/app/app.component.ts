import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { Connection } from 'src/app/models/connection';
import { UserRoleType } from 'src/app/models/enum/UserRoleType';
import { AuthenticationService } from 'src/app/services/authentication.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  UserRoleType = UserRoleType;
  connection?: Connection;

  constructor(
    private router: Router,
    public authenticationService: AuthenticationService
  ) {
    this.connection = this.authenticationService.connection;
    this.authenticationService.onConnectionChange.subscribe(connection => {
      this.connection = connection;
    });
  }

  logout(): void {
    this.authenticationService.logout();
    this.router.navigate(['/login']);
  }
}
