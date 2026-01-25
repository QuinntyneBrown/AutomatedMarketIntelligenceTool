import { Component } from '@angular/core';
import { ShellComponent } from './layout/shell';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [ShellComponent],
  template: '<app-shell />',
  styles: `
    :host {
      display: block;
      height: 100%;
    }
  `,
})
export class App {}
