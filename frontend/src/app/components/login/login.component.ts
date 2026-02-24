import { Component, ElementRef, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { AuthenticationService } from 'src/app/services/authentication.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  username: string = '';
  password: string = '';

  @ViewChild('usernameInput') usernameInput!: ElementRef;
  @ViewChild('passwordInput') passwordInput!: ElementRef;

  constructor(
    private router: Router,
    private authenticationService: AuthenticationService
  ) {}

  focusPassword() {
    this.passwordInput.nativeElement.focus();
  }

  async login() {
    const username = this.username;
    const password = this.password;

    if (username == null || username.length == 0 || password == null || password.length == 0) {
      alert('Please enter both username and password');
      return;
    }

    const result = await this.authenticationService.login(username, password);
    if (result) {
      this.router.navigate(['/devices']);
    }
  }
}
