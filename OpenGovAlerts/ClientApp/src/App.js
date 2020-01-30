import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import { Observer } from './components/Observer';
import { Search } from './components/Search';

export default class App extends Component {
    displayName = App.name

    render() {
        return (
            <Layout>
                <Route exact path='/' component={Home} />
                <Route exact path='/Observer/:id' render={({ match }) => <Observer id={match.params.id} />} />
                <Route exact path='/Observer/:oid/Search/:sid' render={({ match }) => <Search id={match.params.sid} />} />
            </Layout>
        );
    }
}