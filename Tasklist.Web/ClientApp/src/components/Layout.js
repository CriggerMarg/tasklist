import React, { Component } from 'react';
import { Container } from 'reactstrap';
import { NavMenu } from './NavMenu';

export class Layout extends Component {
  static displayName = Layout.name;

  render() {
    return (
      <div style={{ marginTop: 48, marginLeft: 48 }}>
        <Container>{this.props.children}</Container>
      </div>
    );
  }
}
