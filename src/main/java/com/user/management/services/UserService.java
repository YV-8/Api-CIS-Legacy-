package com.user.management.services;

import java.util.List;

import org.modelmapper.ModelMapper;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import com.user.management.dtos.UserResponseDTO;
import com.user.management.repository.UserRepository;

@Service
public class UserService {

    @Autowired
    private  ModelMapper modelMapper;

    @Autowired
    private UserRepository userRepository;
    

    public List<UserResponseDTO> getAllUsers() {
        return userRepository.findAll()
                .stream()
                .map(user -> modelMapper.map(user, UserResponseDTO.class))
                .toList();
    }
    
}
