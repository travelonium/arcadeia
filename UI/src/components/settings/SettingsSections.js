import React, { Component } from 'react';
import { Container } from 'react-bootstrap';

export class Profile extends Component {
    render() {
        return (
            <Container className="m-2" fluid>
                <h2>Profile Settings</h2>
            </Container>
        );
    }
}

export class Account extends Component {
    render() {
        return (
            <Container className="m-2" fluid>
                <h2>Account Settings</h2>
            </Container>
        );
    }
}

export class Preferences extends Component {
    render() {
        return (
            <Container className="m-2" fluid>
                <h2>Preferences Settings</h2>
            </Container>
        );
    }
}