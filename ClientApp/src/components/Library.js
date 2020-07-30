import Row from 'react-bootstrap/Row';
import Col from 'react-bootstrap/Col';
import React, { Component } from 'react';
import Container from 'react-bootstrap/Container';
import Breadcrumb from 'react-bootstrap/Breadcrumb';
import { MediaContainer } from './MediaContainer';

export class Library extends Component {
    static displayName = Library.name;

    constructor(props) {
        super(props);
        this.state = {
            path: props.path != null ? props.path : "/Users/omidontop/Downloads/",
            items: []
        };
    }

    componentDidMount() {
        this.list(this.state.path);
    }

    componentDidUpdate() {
        console.log("updated!");
    }

    list(path) {
        fetch("/list" + path)
            .then(res => res.json())
            .then((items) => {
                this.setState({
                    items: items
                });
            })
    }

    render() {
        return (
            <Container fluid>
                <Row>
                    <Col>
                        <Breadcrumb>
                            {
                                this.state.path.split("/").map((folder) => {
                                    return (
                                        <Breadcrumb.Item href="#">{folder}</Breadcrumb.Item>
                                    );
                                })
                            }
                        </Breadcrumb>
                    </Col>
                </Row>
                <Row xs={1} lg={3}>
                    {
                        this.state.items.map((item) => {
                            return (
                                <Col>
                                    <MediaContainer id={item.id} name={item.name} description="This is a description. It can even span over multiple lines. This one is one example of many." type={item.type} thumbnails={24} />
                                </Col>
                            );
                        })
                    }
                </Row>
            </Container>
        );
    }
}
