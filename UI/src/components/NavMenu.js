import React, { Component } from 'react';
import Container from 'react-bootstrap/Container';
import Navbar from 'react-bootstrap/Navbar';
import Form from 'react-bootstrap/Form';
import Nav from 'react-bootstrap/Nav';
import './NavMenu.css';

export class NavMenu extends Component {
    static displayName = NavMenu.name;

    constructor(props) {
        super(props);
        this.searchTimeout = null;
        this.toggleNavbar = this.toggleNavbar.bind(this);
        this.state = {
            collapsed: true
        };
    }

    onChange(event) {
        let value = event.target.value;
        if (this.searchTimeout != null) {
            clearTimeout(this.searchTimeout);
        }
        this.searchTimeout = setTimeout(() => this.onTimeout(value), 700);
    }

    onTimeout(value) {
        clearTimeout(this.searchTimeout);
        let library = this.props.library.current;
        library.search(value);
    }

    onKeyDown(event) {
    }

    toggleNavbar() {
        this.setState({
            collapsed: !this.state.collapsed
        });
    }

    render() {
        return (
            <header>
                <Navbar collapseOnSelect expand="sm" bg="light" variant="light" className="mb-3 ng-white">
                    <Container>
                        <Navbar.Brand href="/">Media Curator</Navbar.Brand>
                        <Navbar.Toggle onClick={this.toggleNavbar} className="mr-2" label="responsive-navbar-nav" />
                        <Navbar.Collapse id="responsive-navbar-nav" className="d-sm-inline-flex flex-sm-row-reverse">
                            <Nav>
                                <Nav.Item>
                                    <Form.Control ref={this.props.searchInput} onChange={this.onChange.bind(this)} onKeyDown={this.onKeyDown.bind(this)} type="text" placeholder="Search" className=" mr-sm-2" />
                                </Nav.Item>
                            </Nav>
                        </Navbar.Collapse>
                    </Container>
                </Navbar>
            </header>
        );
    }
}