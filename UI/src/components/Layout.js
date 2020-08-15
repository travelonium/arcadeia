import React, { Component } from 'react';
import { Container } from 'reactstrap';
import { NavMenu } from './NavMenu';

export class Layout extends Component {
  static displayName = Layout.name;

  render () {
    return (
      <div className="d-flex flex-column align-content-stretch h-100">
        <NavMenu />
        <Container style={{"flex-grow": "1"}}>
          {this.props.children}
        </Container>
      </div>
    );
  }
}
