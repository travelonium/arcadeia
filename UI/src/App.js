import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Library } from './components/Library';
import { FetchData } from './components/FetchData';
import { Counter } from './components/Counter';

export default class App extends Component {
    static displayName = App.name;

    constructor(props) {
        super(props);
        this.library = React.createRef();
        this.searchForm = React.createRef();
    }

    render() {
        return (
            <Layout library={this.library} searchForm={this.searchForm}>
                <Route exact path='/*' render={(props) => <Library {...props} ref={this.library} searchForm={this.searchForm} /> } />
                <Route path='/counter' component={Counter} />
                <Route path='/fetch-data' component={FetchData} />
            </Layout>
        );
    }
}
