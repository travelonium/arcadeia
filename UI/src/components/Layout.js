import React, { Component } from 'react';
import NavMenu from './NavMenu';

export class Layout extends Component {
  static displayName = Layout.name;

  render () {
    return (
      <div className="d-flex flex-column align-content-stretch h-100">
        <NavMenu library={this.props.library} darkMode={this.props.darkMode} />
        {this.props.children}
      </div>
    );
  }
}
