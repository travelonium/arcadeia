import Row from 'react-bootstrap/Row';
import Col from 'react-bootstrap/Col';
import React, { Component } from 'react';
import Container from 'react-bootstrap/Container';
import Breadcrumb from 'react-bootstrap/Breadcrumb';
import { MediaContainer } from './MediaContainer';
import { extract } from './../utils';

export class Library extends Component {
    static displayName = Library.name;

    constructor(props) {
        super(props);
        this.open = this.open.bind(this);
        let path = "/" + extract("", props, "match", "params", 0);
        this.state = {
            path: path,
            items: []
        };
    }

    componentDidMount() {
        this.list(this.state.path);
    }

    componentDidUpdate() {
    }

    list(path) {
        if (path) {
            fetch("/list" + path)
            .then(response => response.json())
            .then((items) => {
                this.setState({
                    path: path,
                    items: items
                });
                // now change the history!
                window.history.pushState({}, "", path);
            })
        }
    }

    open(source) {
        if (source.type === "Folder") {
            this.list(source.fullPath);
        } else if (source.type == "Video") {
            window.open("/stream/" + source.id + "/" + source.name, "_blank");
        }
    }

    render() {
        let path = "/";
        return (
            <Container fluid>
                <Row>
                    <Col>
                        <Breadcrumb>
                            {
                                "Library".concat(this.state.path).split("/").map((folder, index) => {
                                    if (folder) {
                                        if (index) {
                                            path += folder + "/";
                                        }
                                        return (
                                            <Breadcrumb.Item key={"library-path-item-" + index} href="#" linkProps={{ path: path }} onClick={event => this.list(event.target.getAttribute("path"))} >{folder}</Breadcrumb.Item>
                                        );
                                    } else {
                                        return null;
                                    }
                                })
                            }
                        </Breadcrumb>
                    </Col>
                </Row>
                <Row xs={1} lg={4}>
                    {
                        this.state.items.map((item, index) => {
                            return (
                                <Col key={"media-container-column-index-" + index}>
                                    <MediaContainer source={item} open={this.open} />
                                </Col>
                            );
                        })
                    }
                </Row>
            </Container>
        );
    }
}
